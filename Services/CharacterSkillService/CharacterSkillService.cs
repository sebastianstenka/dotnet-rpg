using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using dotnet_rpg.Data;
using dotnet_rpg.Dtos.Character;
using dotnet_rpg.Dtos.CharacterSkill;
using dotnet_rpg.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace dotnet_rpg.Services.CharacterSkillService
{
  public class CharacterSkillService : ICharacterSkillService
  {
    private readonly DataContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;

    public CharacterSkillService(DataContext context, IHttpContextAccessor httpContextAccessor, IMapper mapper)
    {
      _context = context;
      _httpContextAccessor = httpContextAccessor;
      _mapper = mapper;
    } 

    public async Task<ServiceResponse<GetCharacterDto>> AddCharacterSkill(AddCharacterSkillDto newCharacterSkill)
    {
      var response = new ServiceResponse<GetCharacterDto>();
      try
      {
           var character = await _context.Characters
              .Include(c => c.Weapon)
              .Include(c => c.CharacterSkills).ThenInclude(cs => cs.Skill)
              .FirstOrDefaultAsync(c => c.Id == newCharacterSkill.CharacterId 
              && c.User.Id == int.Parse(_httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)));

              if(character == null)
              {
                response.Success = false;
                response.Message = "Character not found";
                return response;
              }

              var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Id == newCharacterSkill.SkillId);
              if(skill == null)
              {
                response.Success = false;
                response.Message = "Character not found";
                return response;
              }

              var characterSkill = new CharacterSkill
              {
                Character = character,
                Skill = skill
              };

              await _context.CharacterSkills.AddAsync(characterSkill);
              await _context.SaveChangesAsync();

              response.Data = _mapper.Map<GetCharacterDto>(character);
      }
      catch (System.Exception ex)
      {
          response.Success = false;
          response.Message = ex.Message;
      }
      return response;
    }
  }
}