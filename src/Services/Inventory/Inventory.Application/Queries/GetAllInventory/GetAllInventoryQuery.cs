using MediatR;
using Inventory.Application.DTOs;

namespace Inventory.Application.Queries.GetAllInventory;

public record GetAllInventoryQuery : IRequest<IReadOnlyList<InventoryItemResponse>>;
