using System.Threading.Tasks;
using dotnet_rpg.Data;
using dotnet_rpg.Dtos.Fight;
using dotnet_rpg.Models;

namespace dotnet_rpg.Services.FightService
{
  public class FightService : IFightService
  {
    private readonly DataContext _context;
    public FightService(DataContext context)
    {
         _context = context;
    }

    public Task<ServiceResponse<AttackResultDto>> WeaponAttack(WeaponAttackDto request)
    {
       
    }
  }
}