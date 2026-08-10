using System;

namespace TiendaVirtual.Dominio.Servicios.PagoXqm
{
    public static class MensajesRechazoMercadoPago
    {
        public static string ParaUsuario(string? statusDetail, string? paymentMethodId = null)
        {
            var esYape = string.Equals(paymentMethodId, "yape", StringComparison.OrdinalIgnoreCase);
            var detail = (statusDetail ?? string.Empty).ToLowerInvariant();

            return detail switch
            {
                "accredited" => "Pago acreditado correctamente.",
                "pending_contingency" => "Estamos procesando tu pago. Te avisaremos cuando se confirme.",
                "pending_review_manual" => "Tu pago está en revisión. Te notificaremos el resultado.",

                "cc_rejected_bad_filled_card_number" => esYape
                    ? "No se pudo validar el celular Yape. Revisa el número e intenta de nuevo."
                    : "Revisa el número de la tarjeta.",
                "cc_rejected_bad_filled_date" => "Revisa la fecha de vencimiento.",
                "cc_rejected_bad_filled_other" => esYape
                    ? "Revisa el celular y el código de aprobación de Yape."
                    : "Revisa los datos de la tarjeta.",
                "cc_rejected_bad_filled_security_code" => esYape
                    ? "El código de aprobación de Yape no es válido. Genéralo de nuevo en la app."
                    : "Revisa el código de seguridad (CVV).",
                "cc_rejected_blacklist" => esYape
                    ? "Yape no pudo procesar este pago. Prueba más tarde u otro medio."
                    : "No pudimos procesar el pago con esta tarjeta.",
                "cc_rejected_call_for_authorize" => "Debes autorizar el pago con tu banco.",
                "cc_rejected_card_disabled" => "La tarjeta está deshabilitada. Contacta a tu banco.",
                "cc_rejected_card_error" => esYape
                    ? "Yape no pudo completar el pago. Intenta de nuevo."
                    : "No pudimos procesar la tarjeta. Intenta con otra.",
                "cc_rejected_duplicated_payment" => "Ya realizaste un pago por ese monto. Revisa si ya se debitó.",
                "cc_rejected_high_risk" => esYape
                    ? "El pago con Yape fue rechazado por seguridad. Espera un momento e intenta de nuevo."
                    : "Tu pago fue rechazado por seguridad. Prueba otro medio.",
                "cc_rejected_insufficient_amount" => esYape
                    ? "Saldo insuficiente en Yape."
                    : "Fondos insuficientes en la tarjeta.",
                "cc_rejected_invalid_installments" => "La tarjeta no admite esa cantidad de cuotas.",
                "cc_rejected_max_attempts" => esYape
                    ? "Demasiados intentos con Yape. Espera unos minutos e intenta de nuevo."
                    : "Alcanzaste el límite de intentos. Prueba más tarde.",
                "cc_rejected_other_reason" => esYape
                    ? "Yape rechazó el pago. Usa celular 111111111 y código 123456 en pruebas, o verifica tus datos en producción."
                    : "El banco rechazó el pago. Prueba con otra tarjeta.",
                "cc_rejected_card_type_not_allowed" => "Este tipo de tarjeta no está permitido.",
                "cc_rejected_form_error" => esYape
                    ? "Hubo un error al procesar Yape. Genera un nuevo código e intenta de nuevo."
                    : "Hubo un error en el formulario de pago. Inténtalo de nuevo.",

                _ => esYape
                    ? "No se pudo completar el pago con Yape. Verifica celular y código de aprobación e intenta de nuevo."
                    : "No se pudo completar el pago. Intenta con otro medio o más tarde.",
            };
        }
    }
}
