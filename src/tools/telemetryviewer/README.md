# Telemetry Viewer for Open Telemetry Logs, Traces and Metrics

This is a browser based app to view OpenTelemetry logs, traces and metrics that are avaiable as local files. It uses a combination of the [otel-collector](https://github.com/open-telemetry/opentelemetry-collector) and the [.NET Aspire Dashboard](https://github.com/dotnet/aspire/blob/main/src/Aspire.Dashboard/README.md) to achieve this.

## Saving OpenTelemety logs, traces and metrics

While exporting the OpenTelemetry logs, traces and metrics, use the file exporter supported in [otel-collector](https://github.com/open-telemetry/opentelemetry-collector-contrib/blob/main/exporter/fileexporter/README.md). For samples, look at `otel-config.yaml` in `src/scripts/otel-collector`.

Export the logs into the following directory structure
```
  <telemetryFolder>
    \logs
      \service1.log
      \service2.log
      .
      .
    \traces
      \service1.traces
      \service2.traces
      .
      .
    \metrics
      \service1.metrics
      \service2.metrics
      .
      .
```

## Viewing saved logs, traces and metrics

Run the following script:
```pwsh
cd src/tools/telemetryviewer
./run.ps1 -telemetryFolder <Path to the exported telemetry>
```
Open a browser and type in http://localhost:18888 to see the dashboard.

The script creates a docker compose with two images:
 - a local otel-collector image which uses [otlpjsonreceiver](https://github.com/open-telemetry/opentelemetry-collector-contrib/blob/main/receiver/otlpjsonfilereceiver/README.md) to push the telemetry to the aspire dashboard.
 - the aspire dashboard.

Note: The otel-collector only picks up the files from the point of time it has been started, so if the existing content in the files will not be shown in the dashboard. To workaround this, either export the data once the docker compose is up or edit the files. Both will result in the telemetry being refreshed. 