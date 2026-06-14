using Dinder.Application.Subscription.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dinder.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous]
public sealed class WebhookController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhookController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Receive Stripe webhook events.</summary>
    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();
        var stripeSignature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrWhiteSpace(stripeSignature))
            return BadRequest(new { error = "Missing Stripe-Signature header." });

        try
        {
            await _mediator.Send(new ProcessStripeWebhookCommand(json, stripeSignature));
            return Ok();
        }
        catch (Stripe.StripeException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
