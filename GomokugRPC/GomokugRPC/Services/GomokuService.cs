using Grpc.Core;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Data.Common;
using System.Text;

namespace GomokugRPC.Services
{
    public class GomokuService : Gomoku.GomokuBase
    {
        private readonly ILogger<GomokuService> _logger;
        private static Dictionary<Tuple<int, int>, PositionStatus> _board;
        private static string _playerOne, _playerTwo, _nextPlayerWaited;
        private static PositionStatus _winner = PositionStatus.Available;
        private const int sequenceToWin = 5;

        public GomokuService(ILogger<GomokuService> logger)
        {
            _logger = logger;
        }

        public override Task<WantToPlayReply> WantToPlay(WantToPlayRequest request, ServerCallContext context)
        {
            if (_winner != PositionStatus.Available)
            {
                if (_winner == PositionStatus.PlayerOne)
                {
                    return Task.FromResult(new WantToPlayReply
                    {
                        Board = GetBoardString(),
                        NextPlayerWaited = string.Empty,
                        Status = request.PlayerName.Equals(_playerOne) ? BoardStatus.Victory.ToString() : BoardStatus.Defeat.ToString()
                    });
                }
                else if (_winner == PositionStatus.PlayerTwo)
                {
                    return Task.FromResult(new WantToPlayReply
                    {
                        Board = GetBoardString(),
                        NextPlayerWaited = string.Empty,
                        Status = request.PlayerName.Equals(_playerTwo) ? BoardStatus.Victory.ToString() : BoardStatus.Defeat.ToString()
                    });
                }
                else
                {
                    return Task.FromResult(new WantToPlayReply
                    {
                        Board = GetBoardString(),
                        NextPlayerWaited = string.Empty,
                        Status = BoardStatus.Tie.ToString()
                    });
                }
            }
            else if (!string.IsNullOrEmpty(request.Play.Trim()) && !string.IsNullOrEmpty(_nextPlayerWaited) && _nextPlayerWaited.Equals(request.PlayerName))
            {
                var play = request.Play.Split(' ');
                var line = Convert.ToInt16(play[0]);
                var column = Convert.ToInt16(play[1]);
                if (IsValidPlay(line, column))
                {
                    DoValidPlay(line - 1, column - 1, 
                        (request.PlayerName.Equals(_playerOne) ? PositionStatus.PlayerOne : PositionStatus.PlayerTwo));


                    if (HasFinished())
                    {
                        if (_winner == PositionStatus.PlayerOne)
                        {
                            return Task.FromResult(new WantToPlayReply
                            {
                                Board = GetBoardString(),
                                NextPlayerWaited = string.Empty,
                                Status = request.PlayerName.Equals(_playerOne) ? BoardStatus.Victory.ToString() : BoardStatus.Defeat.ToString()
                            });
                        }
                        else if (_winner == PositionStatus.PlayerTwo)
                        {
                            return Task.FromResult(new WantToPlayReply
                            {
                                Board = GetBoardString(),
                                NextPlayerWaited = string.Empty,
                                Status = request.PlayerName.Equals(_playerTwo) ? BoardStatus.Victory.ToString() : BoardStatus.Defeat.ToString()
                            });
                        }
                        else
                        {
                            return Task.FromResult(new WantToPlayReply
                            {
                                Board = GetBoardString(),
                                NextPlayerWaited = string.Empty,
                                Status = BoardStatus.Tie.ToString()
                            });
                        }
                    }
                    else
                    {
                        _nextPlayerWaited = request.PlayerName.Equals(_playerOne) ? (string.IsNullOrEmpty(_playerTwo) ? BoardStatus.WaitingPlayerTwo.ToString() : _playerTwo) : _playerOne;

                        return Task.FromResult(new WantToPlayReply
                        {
                            Board = GetBoardString(),
                            NextPlayerWaited = _nextPlayerWaited ?? string.Empty,
                            Status = (!string.IsNullOrEmpty(_nextPlayerWaited) && _nextPlayerWaited.Equals(_playerOne) ? BoardStatus.WaitingPlayerOne : BoardStatus.WaitingPlayerTwo).ToString()
                        });
                    }
                }
                else
                {
                    return Task.FromResult(new WantToPlayReply
                    {
                        Board = string.Empty,
                        NextPlayerWaited = _nextPlayerWaited,
                        Status = BoardStatus.PlayNotValid.ToString()
                    });
                }
            }
            else if (_board is null)
            {
                GenerateNewBoard();
                _playerOne = request.PlayerName;
                _nextPlayerWaited = _playerOne;

                return Task.FromResult(new WantToPlayReply
                {
                    Board = GetBoardString(),
                    NextPlayerWaited = _nextPlayerWaited,
                    Status = BoardStatus.New.ToString()
                });
            }
            else if (string.IsNullOrEmpty(_playerTwo ?? "") && !_playerOne.Equals(request.PlayerName))
            {
                _playerTwo = request.PlayerName;
                if (_nextPlayerWaited.Equals(BoardStatus.WaitingPlayerTwo.ToString()))
                {
                    _nextPlayerWaited = _playerTwo;
                    return Task.FromResult(new WantToPlayReply
                    {
                        Board = GetBoardString(),
                        NextPlayerWaited = _nextPlayerWaited,
                        Status = (!string.IsNullOrEmpty(_nextPlayerWaited) && _nextPlayerWaited.Equals(_playerOne) ? BoardStatus.WaitingPlayerOne : BoardStatus.WaitingPlayerTwo).ToString()
                    });
                }
                else
                {
                    return Task.FromResult(new WantToPlayReply
                    {
                        Board = string.Empty,
                        NextPlayerWaited = _nextPlayerWaited,
                        Status = (!string.IsNullOrEmpty(_nextPlayerWaited) && _nextPlayerWaited.Equals(_playerOne) ? BoardStatus.WaitingPlayerOne : BoardStatus.WaitingPlayerTwo).ToString()
                    });
                }
            }
            else if (!request.PlayerName.Equals(_playerOne ?? "") && !request.PlayerName.Equals(_playerTwo ?? ""))
            {
                //cada instancia do server controla apenas 1 partida entre 2 players por vez
                //daria para melhorar e fazer varias "salas" ou tabuleiros
                return Task.FromResult(new WantToPlayReply
                {
                    Board = string.Empty,
                    NextPlayerWaited = string.Empty,
                    Status = BoardStatus.BusyServer.ToString()
                });
            }
            else
            {//esperar sua vez
                return Task.FromResult(new WantToPlayReply
                {
                    Board = (request.PlayerName.Equals(_nextPlayerWaited) ? GetBoardString() : string.Empty),
                    NextPlayerWaited = _nextPlayerWaited ?? string.Empty,
                    Status = (!string.IsNullOrEmpty(_nextPlayerWaited) && _nextPlayerWaited.Equals(_playerOne) ? BoardStatus.WaitingPlayerOne : BoardStatus.WaitingPlayerTwo).ToString()
                });
            }
        }

        private Dictionary<Tuple<int, int>, PositionStatus> GenerateNewBoard()
        {
            _board = new Dictionary<Tuple<int, int>, PositionStatus>();
            for (int line = 0; line < 15; line++)
            {
                for (int column = 0; column < 15; column++)
                {
                    _board.Add(new Tuple<int, int>(line, column), PositionStatus.Available);
                }
            }
            return _board;
        }

        private string GetBoardString()
        {
            var sb = new StringBuilder();

            for (int column = 0; column < 15; column++)
            {
                sb.Append($"{(column + 1).ToString().PadLeft(6)}");
            }

            sb.AppendLine();

            for (int line = 0; line < 15; line++)
            {
                for (int column = 0; column < 15; column++)
                {
                    if (column == 0)
                    {
                        sb.Append($"{(line + 1).ToString().PadLeft(3)}");
                    }
                    var position = _board.GetValueOrDefault(new Tuple<int, int>(line, column));
                    switch (position)
                    {
                        case PositionStatus.PlayerOne:
                            sb.Append("o".PadLeft(3));
                            break;
                        case PositionStatus.PlayerTwo:
                            sb.Append("x".PadLeft(3));
                            break;
                        case PositionStatus.Available:
                            sb.Append("*".PadLeft(3));
                            break;
                    }
                    if (column < 14)
                    {
                        sb.Append("-".PadLeft(3));
                    }
                    else
                    {
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        private bool IsValidPlay(int line, int column)
        {
            return line > 0 && line <= 15 && 
                   column > 0 && column <= 15 &&
                   _board.GetValueOrDefault(new Tuple<int, int>(line - 1, column - 1)) == PositionStatus.Available;
        }

        private void DoValidPlay(int line, int column, PositionStatus player)
        {
            _board[new Tuple<int, int>(line, column)] = player;
        }

        private bool HasFinished()
        {
            var verticalCount = 0;
            var horizontalCount = 0;
            var horizontalPlayer = PositionStatus.Available;
            var verticalPlayer = PositionStatus.Available;
            var stillHasAvailablePlays = false;

            //horizontal
            for (int line = 0; line < 15; line++)
            {
                for (int column = 0; column < 15; column++)
                {
                    var horizontalPosition = _board.GetValueOrDefault(new Tuple<int, int>(line, column));
                    if (horizontalPosition == PositionStatus.Available)
                    {
                        horizontalPlayer = horizontalPosition;
                        horizontalCount = 0;
                        stillHasAvailablePlays = true;
                    }
                    else if (horizontalPosition == horizontalPlayer)
                    {
                        horizontalCount++;
                    }
                    else
                    {
                        horizontalCount = 1;
                        horizontalPlayer = horizontalPosition;
                    }

                    if (horizontalCount == sequenceToWin)
                    {
                        _winner = horizontalPosition;
                        return true;
                    }
                }
            }

            //vertical
            for (int column = 0; column < 15; column++)
            {
                for (int line = 0; line < 15; line++)
                {
                    var verticalPosition = _board.GetValueOrDefault(new Tuple<int, int>(line, column));
                    if (verticalPosition == PositionStatus.Available)
                    {
                        verticalPlayer = verticalPosition;
                        verticalCount = 0;
                    }
                    else if (verticalPosition == verticalPlayer)
                    {
                        verticalCount++;
                    }
                    else
                    {
                        verticalCount = 1;
                        verticalPlayer = verticalPosition;
                    }

                    if (verticalCount == sequenceToWin)
                    {
                        _winner = verticalPosition;
                        return true;
                    }
                }
            }

            return !stillHasAvailablePlays;
        }
    }
}
