using Grpc.Core;
using System.Text;

namespace GomokugRPC.Services
{
    public class GomokuService : Gomoku.GomokuBase
    {
        private readonly ILogger<GomokuService> _logger;
        private Dictionary<Tuple<int, int>, PositionStatus> _board;
        private string _playerOne, _playerTwo, _nextPlayerWaited;

        public GomokuService(ILogger<GomokuService> logger)
        {
            _logger = logger;
            _playerOne = string.Empty;
            _playerTwo = string.Empty;
            _nextPlayerWaited = string.Empty;
        }

        public override Task<WantToPlayReply> WantToPlay(WantToPlayRequest request, ServerCallContext context)
        {
            if (_board is null)
            {
                GenerateNewBoard();
                _playerOne = request.PlayerName;
                _nextPlayerWaited = _playerOne;
            }
            else if (string.IsNullOrEmpty(_playerTwo))
            {
                _playerTwo = request.PlayerName;
                _nextPlayerWaited = _playerTwo;
            }
            else if (!request.PlayerName.Equals(_playerOne) && !request.PlayerName.Equals(_playerTwo))
            {
                return Task.FromResult(new WantToPlayReply
                {
                    Board = string.Empty,
                    Status = "Busy server."
                });
            }

            if ((request.PlayerName.Equals(_playerOne) || request.PlayerName.Equals(_playerTwo)) && 
                request.PlayerName.Equals(_nextPlayerWaited))
            {
                ValidPlay(request.Play);
                CheckFinished();
            }
            else 
            {
                //esperar sua vez
                return Task.FromResult(new WantToPlayReply
                {
                    Board = string.Empty,
                    Status = BoardStatus.WaitingOtherPlayer.ToString()
                });
            }

            return Task.FromResult(new WantToPlayReply
            {
                Board = GetBoardString(),
                Status = "status test responding to play " + request.Play
            });
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
                for (int column = 0; column < 15; column++)
                {
                    var position = _board.GetValueOrDefault(new Tuple<int, int>(line, column));

                    switch (position)
                    {
                        case PositionStatus.PlayerOne:
                            sb.Append("○");
                            break;
                        case PositionStatus.PlayerTwo:
                            sb.Append("●");
                            break;
                        default:
                            sb.Append("*");//◙
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
