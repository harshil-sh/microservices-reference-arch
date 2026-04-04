using MediatR;
using Inventory.Application.DTOs;

namespace Inventory.Application.Queries.GetInventoryItem;

public record GetInventoryItemQuery(Guid ProductId) : IRequest<InventoryItemResponse?>;
