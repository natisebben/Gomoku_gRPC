using GomokugRPCClient;
using Grpc.Net.Client;

// The port number must match the port of the gRPC server.
using var channel = GrpcChannel.ForAddress("https://localhost:7090");
var client = new Gomoku.GomokuClient(channel);
var reply = await client.WantToPlayAsync(new WantToPlayRequest { PlayerName = "play test", Play = "12345 play" } );
Console.Write($"{reply.Board} {reply.Status}");
//Console.WriteLine("Press any key to exit...");
Console.ReadKey();