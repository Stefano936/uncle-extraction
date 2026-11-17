using NUnit.Framework;

namespace UncleExtraction.Tests
{
    /// Deteccion de GPU integrada para la calidad por defecto (sin eleccion guardada). Por tipo de dispositivo, no por
    /// fabricante: una Intel Arc o una Radeon RX no son integradas aunque el fabricante coincida.
    public class GraphicsQualityTests
    {
        [TestCase("Intel(R) Iris(R) Xe Graphics", 0x8086, 8043, true)]
        [TestCase("Intel(R) UHD Graphics 620", 0x8086, 1024, true)]
        [TestCase("Intel(R) HD Graphics 4600", 0x8086, 2048, true)]
        [TestCase("Intel(R) Arc(TM) A770 Graphics", 0x8086, 16000, false)]
        [TestCase("Intel(R) Arc(TM) A370M Graphics", 0x8086, 4000, false)]
        [TestCase("NVIDIA GeForce RTX 3070 Ti Laptop GPU", 0x10DE, 8018, false)]
        [TestCase("NVIDIA GeForce MX450", 0x10DE, 2048, false)]
        [TestCase("AMD Radeon(TM) Graphics", 0x1002, 512, true)]
        [TestCase("AMD Radeon Graphics", 0x1002, 2048, true)]
        [TestCase("AMD Radeon(TM) Vega 8 Graphics", 0x1002, 2048, true)]
        [TestCase("AMD Radeon RX 6700 XT", 0x1002, 12000, false)]
        [TestCase("AMD Radeon Pro W6800", 0x1002, 32000, false)]
        [TestCase("Microsoft Basic Render Driver", 0x1414, 0, true)]
        [TestCase("", 0, 0, true)]
        public void Integrated_IsDetectedByDeviceType(string name, int vendor, int mem, bool expected)
            => Assert.AreEqual(expected, GraphicsQuality.IsIntegrated(name, vendor, mem), name);
    }
}
