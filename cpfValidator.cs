using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace fnValidadorCpf
{
    public class cpfValidator
    {
        private readonly ILogger<cpfValidator> _logger;

        public cpfValidator(ILogger<cpfValidator> logger)
        {
            _logger = logger;
        }

        [Function("ValidateCpf")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("Processando validação do CPF.");

            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<RequestData>(requestBody);

            if (string.IsNullOrEmpty(data?.Cpf) || !IsValidCpf(data.Cpf))
            {
                _logger.LogWarning("CPF recebido inválido: {Cpf}", data?.Cpf);
                return new BadRequestObjectResult(new { message = "Formato do CPF inválido.", cpf = data?.Cpf });
            }

            _logger.LogInformation("CPF {Cpf} válido.", data.Cpf);
            return new OkObjectResult(new { message = "CPF válido.", cpf = data.Cpf });
        }

        private static bool IsValidCpf(string cpf)
        {
            cpf = new string(cpf.Where(char.IsDigit).ToArray());
            if (cpf.Length != 11) return false;

            // Validate CPF digits
            int[] multipliers1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] multipliers2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            string tempCpf = cpf[..9];
            int sum = 0;

            for (int i = 0; i < 9; i++) sum += int.Parse(tempCpf[i].ToString()) * multipliers1[i];
            int remainder = (sum * 10) % 11;
            remainder = (remainder == 10) ? 0 : remainder;

            if (remainder != int.Parse(cpf[9].ToString())) return false;

            tempCpf += remainder;
            sum = 0;

            for (int i = 0; i < 10; i++) sum += int.Parse(tempCpf[i].ToString()) * multipliers2[i];
            remainder = (sum * 10) % 11;
            remainder = (remainder == 10) ? 0 : remainder;

            return remainder == int.Parse(cpf[10].ToString());
        }

        private class RequestData
        {
            [JsonPropertyName("cpf")]
            public string? Cpf { get; set; } = "";
        }

    }
}
