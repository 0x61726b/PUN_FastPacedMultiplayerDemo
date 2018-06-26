using UnityEngine;

namespace Assets.Code.Networking.Utils
{
    public class TimeSyncer
    {
        public static int SERVER_STEP_MS = 20; // FixedUpdate frequency
        public static int STEP_MS = 15;

        private float expectedTime;
        private float integrator;
        private float totalDrift;
        private float simulationTime;

        private int serverTicks;
        private readonly float startTime;

        private int simulationTicks;

        public TimeSyncer()
        {
            expectedTime = Time.time + SERVER_STEP_MS / 1000.0f;
            simulationTime = Now();
            startTime = Now();
            totalDrift = 0.0f;
        }

        public bool Move()
        {
            if (Time.time - simulationTime > STEP_MS / 1000.0f)
            {
                simulationTime += STEP_MS / 1000.0f;
                simulationTicks++;
                return true;
            }

            return false;
        }

        public void OnServerUpdate()
        {
            serverTicks++;

            var timeDifference = expectedTime - Now();
            integrator = integrator * 0.9f + timeDifference;

            var adjustment = Mathf.Clamp(integrator * 0.01f, -0.1f, 0.1f);
            totalDrift += adjustment;
            expectedTime += SERVER_STEP_MS / 1000.0f;
        }

        public float Now()
        {
            return Time.time + totalDrift;
        }

        public float ServerDelta(float delta)
        {
            return serverTicks * (SERVER_STEP_MS / 1000.0f) - delta;
        }

        public float TimeSinceStart()
        {
            return Now() - startTime;
        }

    }
}