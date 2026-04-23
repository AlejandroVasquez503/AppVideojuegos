using Microsoft.AspNetCore.Mvc;
using appWeb2.Services;
using appWeb2.Data;
using appWeb2.Models;
using System.Text.Json;
using System.Threading.Tasks;

namespace appWeb2.Controllers
{
    public class PaymentController : Controller
    {
        private readonly PayPalService _paypalService;
        private readonly AppDbContext _context;

        public PaymentController(PayPalService paypalService, AppDbContext context)
        {
            _paypalService = paypalService;
            _context = context;
        }

        /// <summary>
        /// Iniciar proceso de pago con PayPal
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] PaymentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Datos inválidos");

            try
            {
                var returnUrl = Url.Action("ApproveOrder", "Payment", null, Request.Scheme);
                var cancelUrl = Url.Action("CancelOrder", "Payment", null, Request.Scheme);

                var orderId = await _paypalService.CreateOrderAsync(
                    request.Amount,
                    request.Description ?? "Compra de videojuegos",
                    returnUrl,
                    cancelUrl
                );

                return Json(new { id = orderId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Usuario aprobó el pago en PayPal - Capturar orden
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ApproveOrder(string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Token no válido");

            try
            {
                var orderJson = await _paypalService.CaptureOrderAsync(token);

                // Validar estado del pago
                if (orderJson.TryGetProperty("status", out var statusEl) && statusEl.GetString() != "COMPLETED")
                {
                    return RedirectToAction("PaymentFailed");
                }

                // Guardar información de la compra en la BD
                await SavePurchaseAsync(orderJson, token);

                return RedirectToAction("PaymentSuccess", new { orderId = token });
            }
            catch (Exception ex)
            {
                return RedirectToAction("PaymentFailed", new { error = ex.Message });
            }
        }

        /// <summary>
        /// Usuario canceló el pago en PayPal
        /// </summary>
        [HttpGet]
        public IActionResult CancelOrder()
        {
            return RedirectToAction("PaymentCancelled");
        }

        /// <summary>
        /// Página de éxito
        /// </summary>
        [HttpGet]
        public IActionResult PaymentSuccess(string orderId)
        {
            ViewBag.OrderId = orderId;
            ViewBag.Message = "¡Pago realizado exitosamente! Tu compra ha sido procesada.";
            return View();
        }

        /// <summary>
        /// Página de fallo
        /// </summary>
        [HttpGet]
        public IActionResult PaymentFailed(string error = "")
        {
            ViewBag.Error = error ?? "Hubo un error procesando tu pago.";
            return View();
        }

        /// <summary>
        /// Página de cancelación
        /// </summary>
        [HttpGet]
        public IActionResult PaymentCancelled()
        {
            ViewBag.Message = "El pago fue cancelado.";
            return View();
        }

        /// <summary>
        /// Guardar detalles de la compra en la BD
        /// </summary>
        private async Task SavePurchaseAsync(JsonElement paypalOrder, string transactionId)
        {
            try
            {
                // Obtener el UsuarioId de la sesión
                var userIdStr = HttpContext.Session.GetString("UsuarioId");
                if (!int.TryParse(userIdStr, out int usuarioId))
                {
                    throw new Exception("Usuario no autenticado");
                }

                // Crear compra
                var compra = new Compra
                {
                    UsuarioId = usuarioId,
                    FechaCompra = DateTime.Now
                };

                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();

                // Obtener el monto del primer purchase unit
                decimal total = 0;
                if (paypalOrder.TryGetProperty("purchase_units", out var purchaseUnits) && purchaseUnits.ValueKind == System.Text.Json.JsonValueKind.Array && purchaseUnits.GetArrayLength() > 0)
                {
                    var firstUnit = purchaseUnits[0];
                    if (firstUnit.TryGetProperty("amount", out var amountObj) && amountObj.TryGetProperty("value", out var valueEl))
                    {
                        decimal.TryParse(valueEl.GetString(), out total);
                    }
                }

                // Crear detalles de compra (asume que hay datos en sesión sobre los items)
                var detalleCompra = new DetalleCompra
                {
                    idCompra = compra.Id,
                    VideoJuegosId = 1,  // ❌ ESTO ESTÁ HARDCODEADO (siempre es 1)
                    cantidad = 1,       // ❌ ESTO TAMBIÉN ESTÁ HARDCODEADO
                    total = total,      // ✅ Esto sí viene de PayPal
                    estadoCompra = "1",
                    fechaHoraTransaccion = DateTime.Now,
                    codigoTransaccion = transactionId
                };

                _context.detalle_compra.Add(detalleCompra);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando compra: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Modelo para la solicitud de pago
    /// </summary>
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; }
    }
}
