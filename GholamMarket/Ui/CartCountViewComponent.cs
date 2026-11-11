using Application.Store;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GholamMarket.Ui;

public sealed class CartCountViewComponent(IMediator mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var result = await mediator.Send(new GetCartQuery());
        var count = result.IsSuccess ? result.ResultBody?.ItemCount ?? 0 : 0;
        return View(count);
    }
}
