using System;

using OpenSim.Framework;

namespace Diva.Interfaces
{
    public delegate void SceneAction(IScene s);

    public interface ISceneActor
    {
        void ForEachScene(SceneAction d);
    }
}
