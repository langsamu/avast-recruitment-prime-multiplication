﻿namespace WebApplication1.Controllers
{
    using System;
    using System.Linq;
    using System.Threading;
    using ClassLibrary1;
    using Microsoft.AspNetCore.Diagnostics;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;

    [Route("")]
    public class Default : Controller
    {
        private const int timeout = 1000;

        [HttpGet("{thisMany}")]
        public IActionResult Index(int thisMany) =>
            this.View(new PrimeGenerator().Take(thisMany));

        [HttpGet("async/{thisMany}")]
        public IActionResult Cancellable(int thisMany, CancellationToken cancellationToken) =>
            this.CancellableInternal(PrimeGeneratorOptions.ThrowOnCancel, thisMany, cancellationToken);

        [HttpGet("graceful-async/{thisMany}")]
        public IActionResult CancellableGraceful(int thisMany, CancellationToken cancellationToken) =>
            this.CancellableInternal(PrimeGeneratorOptions.None, thisMany, cancellationToken);

        private IActionResult CancellableInternal(PrimeGeneratorOptions options, int thisMany, CancellationToken cancellationToken)
        {
            var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                new CancellationTokenSource(timeout).Token,
                this.HttpContext.RequestAborted);

            this.Response.RegisterForDispose(linkedCancellation);

            var table = MultiplicationTable.GenerateAsync(thisMany, linkedCancellation.Token, options);

            return this.View("Cancellable", table);
        }

        [HttpGet("error")]
        public IActionResult Error()
        {
            var handler = this.HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (handler.Path.StartsWith("/async/") && handler.Error is OperationCanceledException)
            {
                return this.BadRequest($"Couldn't generate {handler.Path[7..]} primes in {timeout}ms. Try less.");
            }
            else
            {
                throw handler.Error;
            }
        }
    }
}
