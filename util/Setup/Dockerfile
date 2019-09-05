FROM mcr.microsoft.com/dotnet/core/runtime:2.2

LABEL com.bytegarden.product="bytegarden" com.bytegarden.project="setup"

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        openssl \
        gosu \
&& rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY obj/Docker/publish .
COPY entrypoint.sh /
RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
