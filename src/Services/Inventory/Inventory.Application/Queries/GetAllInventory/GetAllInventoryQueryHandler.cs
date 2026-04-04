using MediatR;
using Inventory.Application.DTOs;
using Inventory.Application.Mappings;
using Inventory.Domain.Repositories;

namespace Inventory.Application.Queries.GetAllInventory;

public class GetAllInventoryQueryHandler : IRequestHandler<GetAllInventoryQuery, IReadOnlyList<InventoryItemResponse>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetAllInventoryQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IReadOnlyList<InventoryItemResponse>> Handle(GetAllInventoryQuery request, CancellationToken cancellationToken)
    {
        var items = await _inventoryRepository.GetAllAsync(cancellationToken);
        return items.Select(i => i.ToResponse()).ToList();
    }
}
