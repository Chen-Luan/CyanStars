// Code by Gemini 2.5 Pro, unproofread by human beings.

using NUnit.Framework;
using CyanStars.ChartEditor.ViewModel;
using CyanStars.ChartEditor.Model;
using UnityEngine;
using UnityEngine.TestTools;

// 使用 TestFixture 特性标记这个类是一个测试集
[TestFixture]
public class MainViewModelTests
{
    private MainViewModel viewModel;
    private MainModel mockModel;

    // SetUp 方法会在每个测试用例运行前执行
    // 这确保了每个测试都在一个干净、独立的环境中运行
    [SetUp]
    public void SetUp()
    {
        // 对于这个 ViewModel 的测试，我们实际上并不需要一个功能齐全的 MainModel。
        // 传入 null 即可，因为构造函数当前没有使用它。
        // 如果未来构造函数需要与 Model 交互，这里就需要创建一个模拟（Mock）对象。
        mockModel = null;
        viewModel = new MainViewModel(mockModel);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_InitializesPropertiesToDefaultValues()
    {
        // Arrange & Act - 在 SetUp 中已经完成

        // Assert - 验证初始值是否符合预期
        Assert.AreEqual("4", viewModel.PosPrecisionInput);
        Assert.AreEqual("2", viewModel.BeatPrecisionInput);
        Assert.AreEqual("1", viewModel.BeatZoomInput);
    }

    #endregion

    #region PosPrecisionInput Tests

    [Test]
    public void PosPrecisionInput_SetValidValue_UpdatesPropertyAndRaisesEvent()
    {
        // Arrange
        string receivedPropertyName = null;
        viewModel.PropertyChanged += (sender, args) => { receivedPropertyName = args.PropertyName; };
        string newValue = "10";

        // Act
        viewModel.PosPrecisionInput = newValue;

        // Assert
        Assert.AreEqual(newValue, viewModel.PosPrecisionInput, "属性值应该被更新。");
        Assert.AreEqual(nameof(MainViewModel.PosPrecisionInput), receivedPropertyName, "PropertyChanged 事件应该被触发，且属性名正确。");
    }

    // 使用 TestCase 可以用不同的参数多次运行同一个测试方法，减少重复代码
    [TestCase("-1", "负数是非法的")]
    [TestCase("abc", "非数字字符串是非法的")]
    [TestCase("1.5", "浮点数是非法的")]
    [TestCase("", "空字符串是非法的")]
    public void PosPrecisionInput_SetInvalidValue_DoesNotUpdateValueAndRaisesEvent(string invalidValue, string message)
    {
        // Arrange
        string initialValue = viewModel.PosPrecisionInput;
        string receivedPropertyName = null;
        viewModel.PropertyChanged += (sender, args) => { receivedPropertyName = args.PropertyName; };

        // 期望收到一个警告日志
        LogAssert.Expect(LogType.Warning, $"MainViewModel: 输入的值 '{invalidValue}' 不合法，必须为非负整数。");

        // Act
        viewModel.PosPrecisionInput = invalidValue;

        // Assert
        Assert.AreEqual(initialValue, viewModel.PosPrecisionInput, $"属性值不应改变。{message}");
        Assert.AreEqual(nameof(MainViewModel.PosPrecisionInput), receivedPropertyName, "即使输入无效，也应触发 PropertyChanged 以便 UI 刷新回原值。");
    }

    #endregion

    #region BeatPrecisionInput Tests

    [Test]
    public void BeatPrecisionInput_SetValidValue_UpdatesPropertyAndRaisesEvent()
    {
        // Arrange
        string receivedPropertyName = null;
        viewModel.PropertyChanged += (sender, args) => { receivedPropertyName = args.PropertyName; };
        string newValue = "8";

        // Act
        viewModel.BeatPrecisionInput = newValue;

        // Assert
        Assert.AreEqual(newValue, viewModel.BeatPrecisionInput);
        Assert.AreEqual(nameof(MainViewModel.BeatPrecisionInput), receivedPropertyName);
    }

    [TestCase("0", "0 是非法的")]
    [TestCase("-1", "负数是非法的")]
    [TestCase("xyz", "非数字字符串是非法的")]
    public void BeatPrecisionInput_SetInvalidValue_DoesNotUpdateValueAndRaisesEvent(string invalidValue, string message)
    {
        // Arrange
        string initialValue = viewModel.BeatPrecisionInput;
        string receivedPropertyName = null;
        viewModel.PropertyChanged += (sender, args) => { receivedPropertyName = args.PropertyName; };

        LogAssert.Expect(LogType.Warning, $"MainViewModel: 输入的值 '{invalidValue}' 不合法，必须为大于等于1的整数。");

        // Act
        viewModel.BeatPrecisionInput = invalidValue;

        // Assert
        Assert.AreEqual(initialValue, viewModel.BeatPrecisionInput, $"属性值不应改变。{message}");
        Assert.AreEqual(nameof(MainViewModel.BeatPrecisionInput), receivedPropertyName, "即使输入无效，也应触发 PropertyChanged。");
    }

    #endregion

    #region BeatZoomInput Tests

    [TestCase("1.5")]
    [TestCase("0.1")]
    [TestCase("10")]
    public void BeatZoomInput_SetValidValue_UpdatesPropertyAndRaisesEvent(string newValue)
    {
        // Arrange
        string receivedPropertyName = null;
        viewModel.PropertyChanged += (sender, args) => { receivedPropertyName = args.PropertyName; };

        // Act
        viewModel.BeatZoomInput = newValue;

        // Assert
        Assert.AreEqual(newValue, viewModel.BeatZoomInput);
        Assert.AreEqual(nameof(MainViewModel.BeatZoomInput), receivedPropertyName);
    }

    [TestCase("0", "0 是非法的")]
    [TestCase("-1.5", "负数是非法的")]
    [TestCase("hello", "非数字字符串是非法的")]
    public void BeatZoomInput_SetInvalidValue_DoesNotUpdateValueAndRaisesEvent(string invalidValue, string message)
    {
        // Arrange
        string initialValue = viewModel.BeatZoomInput;
        string receivedPropertyName = null;
        viewModel.PropertyChanged += (sender, args) => { receivedPropertyName = args.PropertyName; };

        LogAssert.Expect(LogType.Warning, $"MainViewModel: 输入的值 '{invalidValue}' 不合法，必须为大于0的float。");

        // Act
        viewModel.BeatZoomInput = invalidValue;

        // Assert
        Assert.AreEqual(initialValue, viewModel.BeatZoomInput, $"属性值不应改变。{message}");
        Assert.AreEqual(nameof(MainViewModel.BeatZoomInput), receivedPropertyName, "即使输入无效，也应触发 PropertyChanged。");
    }

    #endregion
}
