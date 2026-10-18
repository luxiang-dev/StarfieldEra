using UnityEngine;
namespace StarfieldEra
{
    /// <summary>A persisted 20-minute day. Daily simulation is shared with sleeping.</summary>
    public sealed class WorldClock:MonoBehaviour
    {
        FarmGame game;float saveClock;
        public void Initialize(FarmGame owner){game=owner;}
        void Update()
        {
            if(!game||game.SimulationPaused)return;
            float dt=Mathf.Min(Time.deltaTime,1);game.State.survival.Tick(dt);
            if(game.State.survival.health<=0){game.Respawn();return;}
            float clockDelta=game.Night.Active?Mathf.Min(dt,Mathf.Max(0,WorldClockState.DaySeconds-game.State.clock.elapsed-.01f)):dt;
            int days=game.State.clock.Tick(clockDelta);
            for(int i=0;i<days;i++)game.AdvanceNewDay(false);
            if(!game.Night.Active&&game.State.clock.IsNight)game.Night.TryBegin(false);
            saveClock+=dt;if(saveClock>=15){saveClock=0;game.Persist();}
        }
    }
}
