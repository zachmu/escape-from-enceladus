using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FarseerPhysics.Dynamics;

namespace Arena.Farseer {

    public static class FarseerExtensions {
        
        public static UserData GetUserData(this Body body) {
            return (UserData) body.UserData;
        }

    }
}
