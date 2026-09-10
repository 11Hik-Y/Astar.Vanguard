using System;

namespace Astar.Vanguard.Client.Runtime
{
    public static class VanguardAiAuthority
    {
        private static Func<bool> _evaluator = () => true;

        public static bool CanApplyOperatorSettings()
        {
            try
            {
                return _evaluator();
            }
            catch
            {
                return false;
            }
        }

        public static void Bind(Func<bool> evaluator)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(
                nameof(evaluator)
            );
        }

        public static void Reset()
        {
            _evaluator = () => true;
        }
    }
}
