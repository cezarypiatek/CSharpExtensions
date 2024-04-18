using System;
using System.Threading.Tasks;

namespace TestNamespace
{
    class SampleClass
    {
        public async Task Test()
        {
            var t2  = CalculateAsync();
            await Step(async () =>
            {
                await t2;
            });
            
        }


        private static Task<int> CalculateAsync() => throw null;
        private static Task Step(Func<Task> func) => throw null;
    }
}