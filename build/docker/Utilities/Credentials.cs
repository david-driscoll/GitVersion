using Cake.Common;
using Cake.Core;
using Common.Utilities;

namespace Docker.Utilities
{
    public class Credentials
    {
        public DockerCredentials? Docker { get; private set; }
        public static Credentials GetCredentials(ICakeContext context, DockerRegistry dockerRegistry) => new()
        {
            Docker = dockerRegistry switch
            {
                DockerRegistry.GitHub =>
                    new DockerCredentials(
                        context.EnvironmentVariable("GITHUB_USERNAME"),
                        context.EnvironmentVariable("GITHUB_TOKEN")),
                _ =>
                    new DockerCredentials(
                        context.EnvironmentVariable("DOCKER_USERNAME"),
                        context.EnvironmentVariable("DOCKER_PASSWORD"))
            }
        };
    }
}