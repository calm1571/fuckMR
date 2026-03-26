using System;
using NUnit.Framework;
using Project.Core;
using Project.Networking;

namespace Project.Tests.EditMode
{
    public abstract class Test5_SynchronizationTestFixture : M0CombatTestFixture
    {
        protected sealed class DummyState : IAppState
        {
            private readonly AppStateId _id;
            private readonly Action _onEnter;

            public DummyState(AppStateId id, Action onEnter = null)
            {
                _id = id;
                _onEnter = onEnter;
            }

            public AppStateId Id => _id;

            public void Enter()
            {
                _onEnter?.Invoke();
            }

            public void Exit()
            {
            }

            public void Tick()
            {
            }
        }

        protected static void SetCalibrationPhase(M0RuntimeBootstrap bootstrap, string phaseName)
        {
            var enumType = typeof(M0RuntimeBootstrap).GetNestedType("LiveCalibrationPhase", System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(enumType, "Failed to find LiveCalibrationPhase enum.");
            var value = Enum.Parse(enumType, phaseName);
            SetPrivateField(bootstrap, "_liveCalibrationPhase", value);
        }

        protected static string GetCalibrationPhase(M0RuntimeBootstrap bootstrap)
        {
            var value = GetPrivateField<object>(bootstrap, "_liveCalibrationPhase");
            return value.ToString();
        }

        protected static AppStateMachine CreateStateMachineWith(params AppStateId[] states)
        {
            var machine = new AppStateMachine();
            for (var i = 0; i < states.Length; i++)
            {
                machine.Register(new DummyState(states[i]));
            }

            return machine;
        }

        protected M0RuntimeBootstrap CreateBootstrapWithState(NetworkRole role, AppStateId initialState, params AppStateId[] additionalStates)
        {
            var bootstrap = CreateBootstrap(role);
            var allStates = new AppStateId[additionalStates.Length + 1];
            allStates[0] = initialState;
            for (var i = 0; i < additionalStates.Length; i++)
            {
                allStates[i + 1] = additionalStates[i];
            }

            var machine = CreateStateMachineWith(allStates);
            machine.ChangeState(initialState);
            SetPrivateField(bootstrap, "_stateMachine", machine);
            return bootstrap;
        }
    }
}
