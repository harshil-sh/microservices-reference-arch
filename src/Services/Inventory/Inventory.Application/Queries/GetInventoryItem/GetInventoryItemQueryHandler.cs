using MediatR;
using Inventory.Application.DTOs;
using Inventory.Application.Mappings;
using Inventory.Domain.Repositories;

namespace Inventory.Application.Queries.GetInventoryItem;

public class GetInventoryItemQueryHandler : IRequestHandler<GetInventoryItemQuery, InventoryItemResponse?>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetInventoryItemQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<InventoryItemResponse?> Handle(GetInventoryItemQuery request, CancellationToken cancellationToken)
    {
        var item = await _inventoryRepository.GetByProductIdAsync(request.ProductId, cancellationToken);
        return item?.ToResponse();
    }
}
