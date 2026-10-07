using System.Collections;

namespace Server.Engines.Craft
{
    public class CraftGroupCol : CollectionBase
    {
        public int Add(CraftGroup craftGroup)
        {
            return List.Add(craftGroup);
        }

        public void Remove(int index)
        {
            if (index > Count - 1 || index < 0)
            {
            }
            else
            {
                List.RemoveAt(index);
            }
        }

        public CraftGroup GetAt(int index)
        {
            return index >= 0 && index < List.Count ? (CraftGroup)List[index] : null;
        }

        public int SearchFor(TextDefinition groupName)
        {
            for (var i = 0; i < List.Count; i++)
            {
                var craftGroup = (CraftGroup)List[i];

                var nameNumber = craftGroup.NameNumber;
                var nameString = craftGroup.NameString;

                if ((nameNumber != 0 && nameNumber == groupName.Number) ||
                    (nameString != null && nameString == groupName.String))
                    return i;
            }

            return -1;
        }
    }
}
