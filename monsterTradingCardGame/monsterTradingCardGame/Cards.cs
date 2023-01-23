using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static monsterTradingCardGame.Card;

namespace monsterTradingCardGame
{
    /// <summary>
    /// This are all the allowed types for cards.
    /// </summary>
    public enum Type
    {
        NotInitialized, 
        spell,
        goblin,
        wizard,
        ork,
        knight,
        kraken,
        elf,
        dragon,
        other
    }

    /// <summary>
    /// This are all the available Elements.
    /// </summary>
    public enum Element
    {
        fire,
        water,
        normal
    }

    /// <summary>
    /// This class is used to store important card information and during battles.
    /// </summary>
    public class Card
    {
        [JsonPropertyName("Id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("Name")]
        public string Name { get; set; }

        //TODO Test mit Set
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Type CType { get; set; }

        [JsonPropertyName("Damage")]
        public double Damage { get; set; }
        public Element Element { get; set; }

        public Card(Guid Id, string Name, double Damage)
        {
            this.Id = Id;
            this.Name = Name;
            this.Damage = Damage;
            this.CType = Type.other;
            this.SetElement();
            this.SetType();

            if(this.CType == Type.other && Name.Contains("elves", StringComparison.OrdinalIgnoreCase))
            {
                this.CType = Type.elf;
            }
            else if(this.CType == Type.other && Name.Contains("wizzard", StringComparison.OrdinalIgnoreCase))
            {
                this.CType = Type.wizard;
            }
        }
        
        public override string ToString()
        {
            return string.Format("Guid: {0},  Name: {1}, Type:{3},   Damage: {2}", Id, Name, Damage, CType);
        }

        /// <summary>
        /// Sets the element for a card.
        /// </summary>
        public void SetElement()
        {
            if (Name.Contains("fire", StringComparison.OrdinalIgnoreCase))
            {
                this.Element = Element.fire;
            }
            else if (Name.Contains("water", StringComparison.OrdinalIgnoreCase))
            {
                this.Element = Element.water;
            }
            else
                this.Element = Element.normal;
        }

        /// <summary>
        /// Sets the type of a card.
        /// </summary>
        public void SetType()
        {
            this.CType = Type.other;
            for (int i = 0; i < Enum.GetNames(typeof(Type)).Length; i++)
            {
                if (Name.ToLower().Contains(Enum.GetName(typeof(Type), i).ToString()))
                {
                    this.CType = (Type)i;
                }
            }
        }

        public bool IsMonster()
        {
            if(this.CType == Type.NotInitialized)
            {
                this.SetType();
            }

            if (CType == Type.spell || CType == Type.NotInitialized)
                return false;
            else
                return true;
        }
    }
}
