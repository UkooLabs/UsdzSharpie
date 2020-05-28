using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace UsdzSharpie
{
    public class UsdcPath
    {
        private string prim_part =  string.Empty; // full path
        private string prop_part = string.Empty; // full path
        private string local_part = string.Empty;
        private bool valid = false;

        public bool IsValid => valid;

        public bool IsEmpty => string.IsNullOrEmpty(prim_part) && string.IsNullOrEmpty(prop_part);

        public static UsdcPath AbsoluteRootPath()
        {
            return new UsdcPath("/");
        }

        public UsdcPath()
        {
            valid = false;
        }

        public UsdcPath(string prim)
        {
            prim_part = prim;
            local_part = prim;
            valid = true;
        }

        public UsdcPath(UsdcPath path)
        {
            prim_part = path.prim_part;
            prop_part = path.prop_part;
            local_part = path.local_part;
            valid = path.valid;
        }

        public string full_path_name()
        {
            var s = string.Empty;
            if (!valid) {
                s += "INVALID#";
            }

            s += prim_part;
            if (string.IsNullOrEmpty(prop_part)) 
            {
                return s;
            }

            s += "." + prop_part;
            return s;
        }

        public string local_path_name()
        {
            var s = string.Empty;
            if (!valid)
            {
                s += "INVALID#";
            }

            s += local_part;
            return s;
        }

        public void SetLocalPath(UsdcPath path)
        {
            local_part = path.local_part;
            valid = path.valid;
        }

        public UsdcPath AppendProperty(string elem)
        {
            var path = new UsdcPath(this);

            if (string.IsNullOrEmpty(elem))
            {
                path.valid = false;
                return path;
            }

            if (elem[0] == '{')
            {
                // variant chars are not supported
                path.valid = false;
                return path;
            }
            else if (elem[0] == '[')
            {
                // relational attrib are not supported
                path.valid = false;
                return path;
            }
            else if (elem[0] == '.')
            {
                //std::cerr << "???. elem[0] is '.'\n";
                // For a while, make this valid.
                path.valid = false;
                return path;
            }
            else
            {
                path.prop_part = elem;
                return path;
            }
        }

        public UsdcPath AppendElement(string elem)
        {
            var path = new UsdcPath(this);

            if (string.IsNullOrEmpty(elem))
            {
                path.valid = false;
                return path;
            }

            if (elem[0] == '{')
            {
                // variant chars are not supported
                path.valid = false;
                return path;
            }
            else if (elem[0] == '[')
            {
                // relational attrib are not supported
                path.valid = false;
                return path;
            }
            else if (elem[0] == '.')
            {
                //std::cerr << "???. elem[0] is '.'\n";
                // For a while, make this valid.
                path.valid = false;
                return path;
            }
            else
            {
                //std::cout << "elem " << elem << "\n";
                if ((path.prim_part.Length == 1) && (path.prim_part[0] == '/'))
                {
                    path.prim_part += elem;
                }
                else
                {
                    path.prim_part += '/' + elem;
                }

                return path;
            }
        }
    }
}
