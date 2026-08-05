# Myll compiler container image.
# Works with Docker and Podman.
#
# The container behaves like the myll compiler itself:
#
#   podman run --rm -v $(pwd):/work -w /work myll example.myll -o out -cr
#
# To run the test suite instead, pass --test:
#
#   podman run --rm myll --test
#
# Build:
#   podman build -t myll .

FROM mcr.microsoft.com/dotnet/sdk:10.0

# Install the latest stable C++ compilers available in Ubuntu 24.04:
# - g++ 14.2 (GCC C++ compiler; pulls in gcc-14 as a dependency)
# - clang++ 20.1 (LLVM C++ compiler)
# Also install antlr4 (pulls in OpenJDK). The Antlr4BuildTasks NuGet package
# regenerates the parser/lexer from the .g4 files during dotnet build.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        g++-14 \
        clang-20 \
        antlr4 \
    && update-alternatives --install /usr/bin/g++ g++ /usr/bin/g++-14 100 \
    && update-alternatives --install /usr/bin/clang++ clang++ /usr/bin/clang++-20 100 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

# The base image only ships the .NET 10 runtime, but the SDK can still build
# projects targeting older frameworks such as net6.0. Roll forward so those
# applications run on the installed .NET 10 runtime.
ENV DOTNET_ROLL_FORWARD=LatestMajor

# Use isolated temp directories for generated output so concurrent test runs
# do not interfere with each other.
ENV MYLL_TEST_TEMP=1

# Build the compiler, run the full test suite once, then clean up runtime
# sockets from /tmp before the layer is committed.
RUN dotnet build myll.sln -c Release \
 && dotnet test testing/ -c Release --no-build \
 && rm -rf /tmp/*

# Put the compiler wrapper on PATH. It picks the actual target-framework
# directory on its own and switches to test mode when called with --test.
COPY scripts/myll-entrypoint.sh /usr/local/bin/myll
RUN chmod +x /usr/local/bin/myll


# Default behavior is the compiler; "myll --help" is shown when no source is
# supplied (or whatever the compiler does with no arguments).
ENTRYPOINT ["myll"]
