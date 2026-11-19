using RoomHub.Api.Domain.Common;
using RoomHub.Api.Domain.Rooms;

namespace RoomHub.Api.Tests.Domain.Rooms;

public sealed class RoomTests
{
    [Fact]
    public void Given_ValidRoomData_When_Creating_Then_ReturnsTrimmedRoom()
    {
        var result = Room.Create("  Room 101  ", 8);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal("Room 101", result.Value.Name);
        Assert.Equal(8, result.Value.Capacity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Given_EmptyName_When_Creating_Then_ReturnsValidationError(string? name)
    {
        var result = Room.Create(name, 8);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Room.InvalidName", result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Given_NonPositiveCapacity_When_Creating_Then_ReturnsValidationError(int capacity)
    {
        var result = Room.Create("Room 101", capacity);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Room.InvalidCapacity", result.Error.Code);
    }
}
