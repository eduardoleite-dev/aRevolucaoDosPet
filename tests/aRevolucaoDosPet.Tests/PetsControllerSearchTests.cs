using System.Security.Claims;
using aRevolucaoDosPet.Controllers;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace aRevolucaoDosPet.Tests;

public class PetsControllerSearchTests
{
    private const string UserId = "507f1f77bcf86cd799439011";
    private const string OtherUserId = "507f1f77bcf86cd799439012";

    [Fact]
    public async Task Search_ForTutor_RestrictsResultsToAuthenticatedUser()
    {
        var petService = new StubPetService();
        var controller = CreateController(petService, UserId);
        var filter = new PetSearchFilter { OwnerId = OtherUserId };

        var result = await controller.Search(filter, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(UserId, petService.SearchFilter?.OwnerId);
    }

    [Fact]
    public async Task Search_WithAdministratorRole_StillRestrictsResultsToAuthenticatedUser()
    {
        var petService = new StubPetService();
        var controller = CreateController(petService, UserId, "ADMINISTRADOR");
        var filter = new PetSearchFilter { OwnerId = OtherUserId };

        var result = await controller.Search(filter, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(UserId, petService.SearchFilter?.OwnerId);
    }

    private static PetsController CreateController(StubPetService petService, string userId, string? role = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        if (role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new PetsController(petService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
                }
            }
        };
    }

    private sealed class StubPetService : IPetService
    {
        public PetSearchFilter? SearchFilter { get; private set; }

        public Task<PagedResult<PetResponseDto>> SearchAsync(
            PetSearchFilter filter,
            CancellationToken cancellationToken = default)
        {
            SearchFilter = filter;
            return Task.FromResult(new PagedResult<PetResponseDto>([], 0, filter.PageNumber, filter.PageSize));
        }

        public Task<PetResponseDto> CreateAsync(
            CreatePetRequest request,
            string? currentUserId,
            bool isAdministrator,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PetResponseDto?> GetByIdAsync(
            string id,
            string? viewerUserId,
            bool isAdministrator,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PetResponseDto?> UpdateAsync(
            string id,
            PetWriteRequest request,
            string? currentUserId,
            bool isAdministrator,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteAsync(
            string id,
            string? currentUserId,
            bool isAdministrator,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> CanEditAsync(
            string id,
            string? userId,
            bool isAdministrator,
            bool ownerOnly = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
