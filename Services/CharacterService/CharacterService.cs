using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using dotnet_rpg.Dtos.Character;
using dotnet_rpg.Models;

namespace dotnet_rpg.Services.CharacterService
{
  public class CharacterService : ICharacterService
  {
    private static List<Character> _characters = new List<Character>()
    {
        new Character(),
        new Character(){Name = "nowy"}
    };
    private readonly IMapper _mapper;

    public CharacterService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task<ServiceResponse<List<GetCharacterDto>>> AddCharacter(AddCharacterDto newCharacter)
    {
        var serviceResponse = new ServiceResponse<List<GetCharacterDto>>();
        var character = _mapper.Map<Character>(newCharacter);
        character.Id = _characters.Max(c => c.Id) + 1;
        _characters.Add(character);
        serviceResponse.Data = _characters.Select(c => _mapper.Map<GetCharacterDto>(c)).ToList();

        return serviceResponse;
    }

    public async Task<ServiceResponse<List<GetCharacterDto>>> GetAllCharacters()
    {
        var serviceResponse = new ServiceResponse<List<GetCharacterDto>>();
        serviceResponse.Data = _characters.Select(c => _mapper.Map<GetCharacterDto>(c)).ToList();

        return serviceResponse;
    }

    public async Task<ServiceResponse<GetCharacterDto>> GetCharacterById(int id)
    {
        var serviceResponse = new ServiceResponse<GetCharacterDto>();
        serviceResponse.Data = _mapper.Map<GetCharacterDto>(_characters.FirstOrDefault(x => x.Id == id));

        return serviceResponse;
    }

    public async Task<ServiceResponse<GetCharacterDto>> UpdateCharacter(UpdateCharacterDto updatedCharacter)
    {
        var serviceResponse = new ServiceResponse<GetCharacterDto>();
        
        try
        {
            var character = _mapper.Map<Character>(_characters.FirstOrDefault(x => x.Id == updatedCharacter.Id));
            character.Class = updatedCharacter.Class;
            character.Defense = updatedCharacter.Defense;
            character.HitPoints = updatedCharacter.HitPoints;
            character.Intelligence = updatedCharacter.Intelligence;
            character.Name = updatedCharacter.Name;
            character.Strength = updatedCharacter.Strength;

            serviceResponse.Data = _mapper.Map<GetCharacterDto>(character);
        }
        catch(Exception ex)
        {
            serviceResponse.Success = false;
            serviceResponse.Message = ex.Message;
        }

        return serviceResponse;
    }


    public async Task<ServiceResponse<List<GetCharacterDto>>> DeleteCharacter(int id)
    {
      var serviceResponse = new ServiceResponse<List<GetCharacterDto>>();

      try
      {
          var character = _mapper.Map<Character>(_characters.FirstOrDefault(x => x.Id == id));
          _characters.Remove(character);
          serviceResponse.Data = _characters.Select(c => _mapper.Map<GetCharacterDto>(c)).ToList();
      }
      catch (Exception ex)
      {
          serviceResponse.Success = false;
          serviceResponse.Message = ex.Message;
      }

      return serviceResponse;    
    }
  }
}