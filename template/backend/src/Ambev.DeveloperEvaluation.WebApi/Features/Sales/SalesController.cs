using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSale;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Application.Sales.DeleteSale;
using Ambev.DeveloperEvaluation.Application.Sales.GetSale;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;
using Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CancelSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CancelSaleItem;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.CreateSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.GetSale;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;
using Ambev.DeveloperEvaluation.WebApi.Features.Sales.UpdateSale;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales;

/// <summary>
/// Gerencia operações de vendas (Sales).
/// Segue o padrão CQRS via MediatR e o padrão External Identities para Cliente e Filial.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SalesController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public SalesController(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    /// <summary>Cria uma nova venda com seus itens.</summary>
    /// <remarks>
    /// Regras de desconto aplicadas automaticamente:
    /// - 1 a 3 itens idênticos → sem desconto
    /// - 4 a 9 itens idênticos → 10% de desconto
    /// - 10 a 20 itens idênticos → 20% de desconto
    /// - Acima de 20 itens → erro de validação
    ///
    /// Exemplo de requisição:
    /// ```json
    /// {
    ///   "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "customerName": "ACME Ltda",
    ///   "branchId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    ///   "branchName": "Filial Centro",
    ///   "saleDate": "2024-12-01T10:00:00Z",
    ///   "items": [
    ///     {
    ///       "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
    ///       "productName": "Produto A",
    ///       "quantity": 10,
    ///       "unitPrice": 29.90
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    [HttpPost]
    [SwaggerOperation(Summary = "Cria uma nova venda", Tags = new[] { "Sales" })]
    [ProducesResponseType(typeof(ApiResponseWithData<CreateSaleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var validator = new CreateSaleRequestValidator();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage).First());

        var command = _mapper.Map<CreateSaleCommand>(request);
        var result = await _mediator.Send(command, cancellationToken);

        return base.Created(string.Empty, new ApiResponseWithData<CreateSaleResponse>
        {
            Success = true,
            Message = "Venda criada com sucesso",
            Data = _mapper.Map<CreateSaleResponse>(result)
        });
    }

    /// <summary>Retorna uma venda pelo seu ID.</summary>
    /// <param name="id">Identificador único da venda (GUID).</param>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Busca venda por ID", Tags = new[] { "Sales" })]
    [ProducesResponseType(typeof(ApiResponseWithData<GetSaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSaleCommand(id), cancellationToken);

        return base.Ok(new ApiResponseWithData<GetSaleResponse>
        {
            Success = true,
            Message = "Venda recuperada com sucesso",
            Data = _mapper.Map<GetSaleResponse>(result)
        });
    }

    /// <summary>Lista vendas com paginação.</summary>
    /// <param name="page">Número da página (mínimo 1). Padrão: 1.</param>
    /// <param name="pageSize">Quantidade de itens por página (1 a 100). Padrão: 10.</param>
    [HttpGet]
    [SwaggerOperation(Summary = "Lista vendas paginadas", Tags = new[] { "Sales" })]
    [ProducesResponseType(typeof(ApiResponseWithData<ListSalesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListSales(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return BadRequest("O número da página deve ser maior que zero.");
        if (pageSize < 1 || pageSize > 100)
            return BadRequest("O tamanho da página deve ser entre 1 e 100.");

        var result = await _mediator.Send(new ListSalesQuery { Page = page, PageSize = pageSize }, cancellationToken);

        return base.Ok(new ApiResponseWithData<ListSalesResponse>
        {
            Success = true,
            Message = "Vendas recuperadas com sucesso",
            Data = _mapper.Map<ListSalesResponse>(result)
        });
    }

    /// <summary>Atualiza uma venda existente substituindo todos os seus itens.</summary>
    /// <param name="id">Identificador único da venda.</param>
    /// <remarks>
    /// Exemplo de requisição:
    /// ```json
    /// {
    ///   "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "customerName": "ACME Ltda",
    ///   "branchId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
    ///   "branchName": "Filial Centro",
    ///   "saleDate": "2024-12-01T10:00:00Z",
    ///   "items": [
    ///     {
    ///       "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
    ///       "productName": "Produto A",
    ///       "quantity": 5,
    ///       "unitPrice": 29.90
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Atualiza uma venda", Tags = new[] { "Sales" })]
    [ProducesResponseType(typeof(ApiResponseWithData<UpdateSaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSale(
        [FromRoute] Guid id,
        [FromBody] UpdateSaleRequest request,
        CancellationToken cancellationToken)
    {
        var validator = new UpdateSaleRequestValidator();
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => e.ErrorMessage).First());

        var command = _mapper.Map<UpdateSaleCommand>(request);
        command.Id = id;
        var result = await _mediator.Send(command, cancellationToken);

        return base.Ok(new ApiResponseWithData<UpdateSaleResponse>
        {
            Success = true,
            Message = "Venda atualizada com sucesso",
            Data = _mapper.Map<UpdateSaleResponse>(result)
        });
    }

    /// <summary>Remove permanentemente uma venda pelo ID.</summary>
    /// <param name="id">Identificador único da venda.</param>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Remove uma venda", Tags = new[] { "Sales" })]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteSaleCommand(id), cancellationToken);
        return base.Ok(new ApiResponse { Success = true, Message = "Venda removida com sucesso" });
    }

    /// <summary>Cancela uma venda. A venda cancelada não pode ser modificada.</summary>
    /// <param name="id">Identificador único da venda.</param>
    [HttpPatch("{id:guid}/cancel")]
    [SwaggerOperation(Summary = "Cancela uma venda", Tags = new[] { "Sales" })]
    [ProducesResponseType(typeof(ApiResponseWithData<CancelSaleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSale([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CancelSaleCommand(id), cancellationToken);

        return base.Ok(new ApiResponseWithData<CancelSaleResponse>
        {
            Success = true,
            Message = "Venda cancelada com sucesso",
            Data = _mapper.Map<CancelSaleResponse>(result)
        });
    }

    /// <summary>Cancela um item específico dentro de uma venda.</summary>
    /// <param name="saleId">Identificador da venda.</param>
    /// <param name="itemId">Identificador do item a cancelar.</param>
    [HttpPatch("{saleId:guid}/items/{itemId:guid}/cancel")]
    [SwaggerOperation(Summary = "Cancela um item da venda", Tags = new[] { "Sales" })]
    [ProducesResponseType(typeof(ApiResponseWithData<CancelSaleItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSaleItem(
        [FromRoute] Guid saleId,
        [FromRoute] Guid itemId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CancelSaleItemCommand { SaleId = saleId, ItemId = itemId },
            cancellationToken);

        return base.Ok(new ApiResponseWithData<CancelSaleItemResponse>
        {
            Success = true,
            Message = "Item cancelado com sucesso",
            Data = _mapper.Map<CancelSaleItemResponse>(result)
        });
    }
}
