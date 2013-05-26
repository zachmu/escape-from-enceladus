using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Enceladus.Event {

    /// <summary>
    /// An object that knows how to load and save itself.
    /// </summary>
    public interface ISaveable {
        void Save(SaveState save);
        void LoadFromSave(SaveState save);
    }
}
