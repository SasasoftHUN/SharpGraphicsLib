using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Utils.OBJ
{
    public class OBJModel
    {

        public OBJModelGroup[] Groups { get; }
        public OBJMaterial[] Materials { get; }

        internal OBJModel(OBJModelGroup[] groups, OBJMaterial[] materials)
        {
            Groups = groups;
            Materials = materials;
        }

    }
}
