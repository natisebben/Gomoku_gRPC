using GomokugRPCClient;
using Grpc.Net.Client;

// The port number must match the port of the gRPC server.
var playerName = new Random().Next(100);

using var channel = GrpcChannel.ForAddress("https://localhost:7090");
var client = new Gomoku.GomokuClient(channel);
var status = string.Empty;

do
{
    var play = string.Empty;
    if (status.Equals(BoardStatus.Ready.ToString()) ||
        status.Equals(BoardStatus.New.ToString()))
    {
        Console.WriteLine("Your turn: ");
        play = Console.ReadLine();
    }

    var reply = await client.WantToPlayAsync(new WantToPlayRequest { PlayerName = $"Player {playerName}", Play = play });
    status = reply.Status;

    if (status.Equals(BoardStatus.New.ToString()))
    {
        Console.WriteLine("A new match has started:\n");
    }

    Console.Write($"{reply.Board}\n");

} while (!status.Equals(BoardStatus.Defeat.ToString()) && !status.Equals(BoardStatus.Victory.ToString()));

if (status.Equals(BoardStatus.Defeat.ToString()))
{
    Console.WriteLine("You Lost!");
}
else
{
    Console.WriteLine("You Won!");
}
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
