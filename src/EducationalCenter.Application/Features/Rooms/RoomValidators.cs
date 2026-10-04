using FluentValidation;

namespace EducationalCenter.Application.Features.Rooms;

public sealed class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 1000);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Location).MaximumLength(200);
    }
}

public sealed class UpdateRoomRequestValidator : AbstractValidator<UpdateRoomRequest>
{
    public UpdateRoomRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 1000);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Location).MaximumLength(200);
    }
}

public sealed class RoomListQueryValidator : AbstractValidator<RoomListQuery>
{
    public RoomListQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
