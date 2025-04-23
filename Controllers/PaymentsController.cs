using Microsoft.AspNetCore.Mvc;
using MarketplaceAPI.Models;
using Stripe;
using Stripe.Checkout;

namespace MarketplaceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly StripeSettings _stripeSettings;
        private readonly PaymentIntentService _intentService;

        public PaymentsController(StripeSettings stripeSettings)
        {
            _stripeSettings = stripeSettings;
            _intentService = new PaymentIntentService();
        }

        [HttpPost("create-payment-intent")]
        public IActionResult Create([FromBody] CreatePaymentIntentRequest req)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = req.Amount,
                Currency = req.Currency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };
            var intent = _intentService.Create(options);
            return Ok(new { clientSecret = intent.ClientSecret });
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _stripeSettings.WebhookSecret
                );

                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var intent = stripeEvent.Data.Object as PaymentIntent;
                    // TODO: сохранить информацию об успешной оплате
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest($"Webhook error: {e.Message}");
            }
        }
    }

    public class CreatePaymentIntentRequest
    {
        public long Amount { get; set; }      // в центах
        public string Currency { get; set; }  // напр. "usd"
    }
}
