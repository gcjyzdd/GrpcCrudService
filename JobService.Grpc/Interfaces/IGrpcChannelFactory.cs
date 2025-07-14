using Grpc.Net.Client;

namespace JobService.Grpc.Interfaces;

public interface IGrpcChannelFactory
{
    GrpcChannel CreateChannel();
}