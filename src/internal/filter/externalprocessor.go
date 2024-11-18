package filter

import (
	"context"
	"fmt"
	"io"
	"reflect"

	log "github.com/sirupsen/logrus"
	"go.opentelemetry.io/otel/trace"

	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"

	pb "github.com/envoyproxy/go-control-plane/envoy/service/ext_proc/v3"
	typev3 "github.com/envoyproxy/go-control-plane/envoy/type/v3"
)

func NewExternalProcessorServer(tracer trace.Tracer, ff HttpFilterFactory) pb.ExternalProcessorServer {
	return &externalProcessor{
		tracer:        tracer,
		filterFactory: ff,
	}
}

type externalProcessor struct {
	tracer        trace.Tracer
	filterFactory HttpFilterFactory
}

func (s *externalProcessor) Process(srv pb.ExternalProcessor_ProcessServer) error {
	// Create a new httpFilter instance for processing this proxy request stream.
	httpFilter := s.filterFactory.CreateFilter()
	ctx := srv.Context()
	for {
		var err error
		defer func() {
			if err != io.EOF {
				RecordSpanError(ctx, &err)
			}
		}()

		select {
		case <-ctx.Done():
			return ctx.Err()
		default:
		}

		// Process the gRPC request asynchronously by connecting to its bi-directional stream.
		req, err := srv.Recv()
		if err == io.EOF {
			return nil
		}
		if err != nil {
			return status.Errorf(codes.Unknown, "cannot receive stream request: %v", err)
		}

		ctx2, requestSpan := s.tracer.Start(ctx, reflect.TypeOf(req.Request).String())
		defer requestSpan.End()

		// Handle the next proxy request from the stream.
		resp := handleProxyRequest(ctx2, httpFilter, req)

		err = srv.Send(resp)
		if err != nil {
			log.Errorf("send error %v", err)
			return status.Errorf(codes.Unknown, "cannot send stream response: %v", err)
		}
	}
}

func handleProxyRequest(
	ctx context.Context,
	httpFilter HttpFilter,
	req *pb.ProcessingRequest) *pb.ProcessingResponse {
	switch v := req.Request.(type) {
	case *pb.ProcessingRequest_RequestHeaders:
		log.Debugf("Handling proxy request: %v", v)
		return httpFilter.OnRequestHeaders(ctx, req)

	case *pb.ProcessingRequest_RequestBody:
		return httpFilter.OnRequestBody(ctx, req)

	case *pb.ProcessingRequest_ResponseHeaders:
		log.Debugf("Handling proxy response: %v", v)
		return httpFilter.OnResponseHeaders(ctx, req)

	case *pb.ProcessingRequest_ResponseBody:
		return httpFilter.OnResponseBody(ctx, req)

	default:
		return CreateErrorProxyResponse(
			typev3.StatusCode_BadRequest,
			fmt.Sprintf("unexpected processing request type %T", v))
	}
}
