using Grpc.Net.Client;

namespace JobService.Client.Interfaces;

public interface IGrpcChannelFactory
{
    GrpcChannel CreateChannel();
}