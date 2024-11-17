// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc.Formatters;

public class ByteArrayInputFormatter : InputFormatter
{
    public ByteArrayInputFormatter()
    {
        this.SupportedMediaTypes.Add(Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(
            "application/cose"));
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context)
    {
        var stream = new MemoryStream();
        await context.HttpContext.Request.Body.CopyToAsync(stream);
        return await InputFormatterResult.SuccessAsync(stream.ToArray());
    }

    protected override bool CanReadType(Type type)
    {
        return type == typeof(byte[]);
    }
}