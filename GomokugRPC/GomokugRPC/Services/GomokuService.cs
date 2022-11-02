using Grpc.Core;
using System.Text;

namespace GomokugRPC.Services
{
    public class GomokuService : Gomoku.GomokuBase
    {
        private readonly ILogger<GomokuService> _logger;
        private static Dictionary<Tuple<int, int>, PositionStatus> _board;
        private static string _playerOne, _playerTwo, _nextPlayerWaited;
        private static char[] _alphabet = new char[15] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O'};


        public GomokuService(ILogger<GomokuService> logger)
        {
            _logger = logger;
        }

        public override Task<WantToPlayReply> WantToPlay(WantToPlayRequest request, ServerCallContext context)
        {
            if (!string.IsNullOrEmpty(_nextPlayerWaited) && _nextPlayerWaited.Equals(request.PlayerName))
            {
                //validar a jogada e fazer a jogada
                //trocar o player esperado para o 2, se existir
                ValidPlay(request.Play);
                CheckFinished();
                return Task.FromResult(new WantToPlayReply
                {
                    Board = GetBoardString(),
                    Status = "trocar para a resposta adequada pra essa situação " + request.Play
                });
            }
            else if (_board is null)
            {
                GenerateNewBoard();
                _playerOne = request.PlayerName;
                _nextPlayerWaited = _playerOne;

                return Task.FromResult(new WantToPlayReply
                {
                    Board = GetBoardString(),
                    Status = BoardStatus.New.ToString()
                });
            }
            else if (string.IsNullOrEmpty(_playerTwo ?? ""))
            {
                _playerTwo = request.PlayerName;
                _nextPlayerWaited = _playerTwo;

                return Task.FromResult(new WantToPlayReply
                {
                    Board = GetBoardString(),
                    Status = "trocar para a resposta certa pra essa situação " + request.Play
                });
            }
            else if (!request.PlayerName.Equals(_playerOne ?? "") && !request.PlayerName.Equals(_playerTwo ?? ""))
            {
                //cada instancia do server controla apenas 1 partida entre 2 players por vez
                return Task.FromResult(new WantToPlayReply
                {
                    Board = string.Empty,
                    Status = "Busy server."
                });
            }
            else
            {//esperar sua vez
                return Task.FromResult(new WantToPlayReply
                {
                    Board = string.Empty,
                    Status = BoardStatus.WaitingOtherPlayer.ToString()
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

            for (int line = 0; line < 15; line++)
            {
                sb.Append($"{(line + 1).ToString().PadLeft(2)}");
                for (int column = 0; column < 15; column++)
                {
                    var position = _board.GetValueOrDefault(new Tuple<int, int>(line, column));

                    if (line == 0)
                    {
                        sb.Append($"{_alphabet[column]}\n");
                    }
                    switch (position)
                    {
                        case PositionStatus.PlayerOne:
                            sb.Append("○");
                            break;
                        case PositionStatus.PlayerTwo:
                            sb.Append("●");
                            break;
                        case PositionStatus.Available:
                            sb.Append("*");
                            break;
                    }
                    if (column < 14)
                    {
                        sb.Append("-");
                    }
                    else
                    {
                        sb.Append("\n");
                    }
                }
            }

            return sb.ToString();
        }

        private bool ValidPlay(string play)
        {
            return true;
        }

        private bool CheckFinished()
        {
            return false;
        }

        private string GetStatus()
        {
            CheckFinished();
            return "status";
        }
    }
}
