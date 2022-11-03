using GomokugRPCClient;
using Grpc.Net.Client;

// The port number must match the port of the gRPC server.
var playerName = $"Player {new Random().Next(100)}";

using var channel = GrpcChannel.ForAddress("https://localhost:7090");
var client = new Gomoku.GomokuClient(channel);
var status = string.Empty;
var nextPlayerWaited = string.Empty;

do
{
    if (status.Equals(BoardStatus.BusyServer.ToString()))
    {
        break;
    }
    var line = string.Empty;
    var column = string.Empty;

    if (nextPlayerWaited.Equals(playerName))
    {
        Console.WriteLine("Your turn!");
        Console.Write("Line: ");
        line = Console.ReadLine();
        Console.Write("Column: ");
        column = Console.ReadLine();
    }

    var reply = await client.WantToPlayAsync(new WantToPlayRequest { PlayerName = $"{playerName}", Play = $"{line} {column}" });

    if (reply.Status.Equals(BoardStatus.New.ToString()))
    {
        Console.WriteLine($"You ({playerName}) has started a new match:\n");
    }
    else if (reply.Status.Equals(BoardStatus.PlayNotValid.ToString()))
    {
        Console.WriteLine("\nThis play is not valid.\n");
    }

    if (!string.IsNullOrEmpty(reply.Board))
    {
        Console.Write($"{reply.Board}\n");
    }

    if (!reply.Status.Equals(BoardStatus.BusyServer.ToString()) && 
        !reply.Status.Equals(BoardStatus.Victory.ToString()) && 
        !reply.Status.Equals(BoardStatus.Defeat.ToString()) && 
        !playerName.Equals(reply.NextPlayerWaited) && !status.Equals(reply.Status))
    {
        Console.WriteLine($"Waiting for {(!string.IsNullOrEmpty(reply.NextPlayerWaited) && !reply.NextPlayerWaited.Equals(BoardStatus.WaitingPlayerTwo.ToString()) ? reply.NextPlayerWaited : "another player")}");
    }

    status = reply.Status;
    nextPlayerWaited = reply.NextPlayerWaited;

} while (!status.Equals(BoardStatus.BusyServer.ToString()) && !status.Equals(BoardStatus.Defeat.ToString()) && !status.Equals(BoardStatus.Victory.ToString()));

if (status.Equals(BoardStatus.Defeat.ToString()))
{
    Console.WriteLine("You Lost!");
}
else if (status.Equals(BoardStatus.Victory.ToString()))
{
    Console.WriteLine("You Won!");
}
else
{
    Console.WriteLine("Server is busy, try again later.");
}
Console.WriteLine("Press any key to exit...");
Console.ReadKey();