namespace CyanStars.Framework.Dialogue
{
    public interface IFakeService
    {
        public void TestFunc1();
    }

    public class FakeService : IFakeService
    {
        public void TestFunc1()
        {
            throw new System.NotImplementedException();
        }
    }
}
