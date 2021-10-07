using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using dotnet_rpg.Data;
using dotnet_rpg.Dtos.Fight;
using dotnet_rpg.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_rpg.Services.FightService
{
  public class FightService : IFightService
  {
    private readonly DataContext _context;
    private readonly IMapper _mapper;

    public FightService(DataContext context, IMapper mapper)
    {
         _context = context;
         _mapper = mapper;
    }

    public async Task<ServiceResponse<FightResultDto>> Fight(FightRequestDto request)
    {
        var response = new ServiceResponse<FightResultDto>
        {
            Data = new FightResultDto()
        };

        try
        {
             var characters = await _context.Characters
             .Include(c => c.Weapon)
             .Include(c => c.CharacterSkills).ThenInclude(cs => cs.Skill)
             .Where(c => request.CharacterIds.Contains(c.Id)).ToListAsync();

             var defeated = false;
             while(!defeated)
             {
                 foreach (var attacker in characters)
                 {
                    var opponents = characters.Where(c => c.Id != attacker.Id).ToList();
                    var opponent = opponents[new Random().Next(opponents.Count)];

                    var damage = 0;
                    var attackUsed = string.Empty;

                    var useWeapon = new Random().Next(2) == 0;
                    if(useWeapon)
                    {
                        attackUsed = attacker.Weapon.Name;
                        damage = DoWeaponAttack(attacker, opponent);
                    }
                    else
                    {
                        var randomSkill = new Random().Next(attacker.CharacterSkills.Count);
                        attackUsed = attacker.CharacterSkills[randomSkill].Skill.Name;
                        damage = DoSkillAttack(attacker, opponent, attacker.CharacterSkills[randomSkill]);
                    }

                    response.Data.Log.Add($"{attacker.Name} attacks {opponent.Name} using {attackUsed} with {(damage >= 0 ? damage : 0)}");

                    if(opponent.HitPoints <= 0 )
                    {
                        defeated = true;
                        attacker.Victories++;
                        opponent.Defeats--;
                        response.Data.Log.Add($"{opponent.Name} has been defeated!");
                        response.Data.Log.Add($"{attacker.Name} wins!");
                        break;
                    }
                 }  
             }

            characters.ForEach(c =>
            {
                c.Fights++;
                c.HitPoints = 100;
            });

            _context.Characters.UpdateRange(characters);
            await _context.SaveChangesAsync();
        }
        catch (System.Exception ex)
        {
            response.Success = false;
            response.Message = ex.Message;
        }
        return response;
    }

    public async Task<ServiceResponse<AttackResultDto>> SkillAttack(SkillAttackDto request)
    {
        var response = new ServiceResponse<AttackResultDto>();
        try
      {
        var attacker = await _context.Characters
            .Include(c => c.CharacterSkills).ThenInclude(cs => cs.Skill)
            .FirstOrDefaultAsync(c => c.Id == request.AttackerId);

        var opponent = await _context.Characters.FirstOrDefaultAsync(c => c.Id == request.OpponentId);

        var characterSkill = attacker.CharacterSkills.FirstOrDefault(cs => cs.Skill.Id == request.SkillId);
        if (characterSkill == null)
        {
          response.Success = false;
          response.Message = $"{attacker.Name} doesn't know that skill.";
          return response;
        }

        var damage = DoSkillAttack(attacker, opponent, characterSkill);

        if (opponent.HitPoints <= 0)
        {
          response.Message = $"{opponent.Name} has been defeated!";
        }

        _context.Characters.Update(opponent);
        await _context.SaveChangesAsync();

        response.Data = new AttackResultDto
        {
          Attacker = attacker.Name,
          AttackerHp = attacker.HitPoints,
          Opponent = opponent.Name,
          OpponentHp = opponent.HitPoints,
          Damage = damage
        };
      }
      catch (System.Exception ex)
        {
            response.Success = false;
            response.Message = ex.Message;
        }
        return response;
    }

    private static int DoSkillAttack(Character attacker, Character opponent, CharacterSkill characterSkill)
    {
      var damage = characterSkill.Skill.Damage + (new Random().Next(attacker.Intelligence));
      damage -= new Random().Next(opponent.Defense);
      if (damage > 0)
      {
        opponent.HitPoints -= damage;
      }

      return damage;
    }

    private static int DoWeaponAttack(Character attacker, Character opponent)
    {
      var damage = attacker.Weapon.Damage + (new Random().Next(attacker.Strength));
      damage -= new Random().Next(opponent.Defense);
      if (damage > 0)
      {
        opponent.HitPoints -= damage;
      }

      return damage;
    }

    public async Task<ServiceResponse<AttackResultDto>> WeaponAttack(WeaponAttackDto request)
    {
       var response = new ServiceResponse<AttackResultDto>();

       try
      {
        var attacker = await _context.Characters
            .Include(c => c.Weapon)
            .FirstOrDefaultAsync(c => c.Id == request.AttackerId);

        var opponent = await _context.Characters.FirstOrDefaultAsync(c => c.Id == request.OpponentId);
        var damage = DoWeaponAttack(attacker, opponent);

        if (opponent.HitPoints <= 0)
        {
          response.Message = $"{opponent.Name} has been defeated!";
        }

        _context.Characters.Update(opponent);
        await _context.SaveChangesAsync();

        response.Data = new AttackResultDto
        {
          Attacker = attacker.Name,
          AttackerHp = attacker.HitPoints,
          Opponent = opponent.Name,
          OpponentHp = opponent.HitPoints,
          Damage = damage
        };
      }
      catch (System.Exception ex)
       {
           response.Success = false;
           response.Message = ex.Message;
       }
       return response;
    }

    public async Task<ServiceResponse<List<HighscoreDto>>> GetHighscore()
    {
       var characters = await _context.Characters
            .Where(c => c.Fights > 0)
            .OrderByDescending(c => c.Victories)
            .ThenBy(c => c.Defeats)
            .ToListAsync();

        var response = new ServiceResponse<List<HighscoreDto>>
        {
            Data = characters.Select(c => _mapper.Map<HighscoreDto>(c)).ToList()
        };

        return response;
    }
  }
}