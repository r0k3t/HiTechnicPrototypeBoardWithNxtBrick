//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18034
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

[assembly: global::System.Reflection.AssemblyVersionAttribute("0.0.0.0")]
[assembly: global::Microsoft.Dss.Core.Attributes.ServiceDeclarationAttribute(global::Microsoft.Dss.Core.Attributes.DssServiceDeclaration.Transform, SourceAssemblyKey="User.MindSensors.Y2007.M07, Version=0.0.0.0, Culture=neutral, PublicKeyToken=7721" +
    "87607e5e5359")]
[assembly: global::System.Security.SecurityTransparentAttribute()]
[assembly: global::System.Security.SecurityRulesAttribute(global::System.Security.SecurityRuleSet.Level1)]

namespace Dss.Transforms.TransformUser {
    
    
    public class Transforms : global::Microsoft.Dss.Core.Transforms.TransformBase {
        
        static Transforms() {
            Register();
        }
        
        public static void Register() {
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassConfig), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassConfig_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassConfig));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassConfig), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassConfig_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassConfig));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassSensorState), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassSensorState_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassSensorState));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassSensorState), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassSensorState_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassSensorState));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassReading), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassReading_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassReading));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassReading), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassReading_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassReading));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.I2CReadMindSensorsCompassSensor), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CReadMindSensorsCompassSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CReadMindSensorsCompassSensor));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.I2CReadMindSensorsCompassSensor), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CReadMindSensorsCompassSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CReadMindSensorsCompassSensor));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.I2CInitializeMindSensorsCompass), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CInitializeMindSensorsCompass_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CInitializeMindSensorsCompass));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.I2CInitializeMindSensorsCompass), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CInitializeMindSensorsCompass_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CInitializeMindSensorsCompass));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.I2CResponseMindSensorsCompassSensor), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CResponseMindSensorsCompassSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CResponseMindSensorsCompassSensor));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.I2CResponseMindSensorsCompassSensor), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CResponseMindSensorsCompassSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CResponseMindSensorsCompassSensor));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.AccelerometerConfig), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_AccelerometerConfig_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_AccelerometerConfig));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.AccelerometerConfig), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_AccelerometerConfig_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_AccelerometerConfig));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.AccelerometerState), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_AccelerometerState_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_AccelerometerState));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.AccelerometerState), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_AccelerometerState_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_AccelerometerState));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.I2CReadMindSensorsAccelerationSensor), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_I2CReadMindSensorsAccelerationSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_I2CReadMindSensorsAccelerationSensor));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.I2CReadMindSensorsAccelerationSensor), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_I2CReadMindSensorsAccelerationSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_I2CReadMindSensorsAccelerationSensor));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddProxyTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.I2CResponseMindSensorsAccelerationSensor), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_I2CResponseMindSensorsAccelerationSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_I2CResponseMindSensorsAccelerationSensor));
            global::Microsoft.Dss.Core.Transforms.TransformBase.AddSourceTransform(typeof(global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.I2CResponseMindSensorsAccelerationSensor), new global::Microsoft.Dss.Core.Attributes.Transform(Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_I2CResponseMindSensorsAccelerationSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_I2CResponseMindSensorsAccelerationSensor));
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassConfig_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassConfig(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassConfig target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassConfig();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassConfig from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassConfig)(transformFrom));
            target.Name = from.Name;
            target.SensorPort = from.SensorPort;
            target.PollingFrequencyMs = from.PollingFrequencyMs;
            return target;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassConfig_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassConfig(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassConfig target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassConfig();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassConfig from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassConfig)(transformFrom));
            target.Name = from.Name;
            target.SensorPort = from.SensorPort;
            target.PollingFrequencyMs = from.PollingFrequencyMs;
            return target;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassSensorState_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassSensorState(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassSensorState target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassSensorState();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassSensorState from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassSensorState)(transformFrom));
            target.Connected = from.Connected;
            target.Name = from.Name;
            target.SensorPort = from.SensorPort;
            target.PollingFrequencyMs = from.PollingFrequencyMs;
            if ((from.Heading != null)) {
                target.Heading = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassReading)(Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassReading_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassReading(from.Heading)));
            }
            else {
                target.Heading = null;
            }
            return target;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassSensorState_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassSensorState(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassSensorState target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassSensorState();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassSensorState from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassSensorState)(transformFrom));
            target.Connected = from.Connected;
            target.Name = from.Name;
            target.SensorPort = from.SensorPort;
            target.PollingFrequencyMs = from.PollingFrequencyMs;
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassReading tmp = from.Heading;
            if ((tmp != null)) {
                target.Heading = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassReading)(Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassReading_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassReading(tmp)));
            }
            return target;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassReading_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassReading(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassReading target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassReading();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassReading from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassReading)(transformFrom));
            target.Degrees = from.Degrees;
            target.TimeStamp = from.TimeStamp;
            return target;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_CompassReading_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_CompassReading(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassReading target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.CompassReading();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassReading from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.CompassReading)(transformFrom));
            target.Degrees = from.Degrees;
            target.TimeStamp = from.TimeStamp;
            return target;
        }
        
        private static global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.I2CReadMindSensorsCompassSensor _cachedInstance0 = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.I2CReadMindSensorsCompassSensor();
        
        private static global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.I2CReadMindSensorsCompassSensor _cachedInstance = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.I2CReadMindSensorsCompassSensor();
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CReadMindSensorsCompassSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CReadMindSensorsCompassSensor(object transformFrom) {
            return _cachedInstance;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CReadMindSensorsCompassSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CReadMindSensorsCompassSensor(object transformFrom) {
            return _cachedInstance0;
        }
        
        private static global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.I2CInitializeMindSensorsCompass _cachedInstance2 = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.I2CInitializeMindSensorsCompass();
        
        private static global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.I2CInitializeMindSensorsCompass _cachedInstance1 = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.I2CInitializeMindSensorsCompass();
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CInitializeMindSensorsCompass_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CInitializeMindSensorsCompass(object transformFrom) {
            return _cachedInstance1;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CInitializeMindSensorsCompass_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CInitializeMindSensorsCompass(object transformFrom) {
            return _cachedInstance2;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CResponseMindSensorsCompassSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CResponseMindSensorsCompassSensor(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.I2CResponseMindSensorsCompassSensor target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.I2CResponseMindSensorsCompassSensor();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.I2CResponseMindSensorsCompassSensor from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.I2CResponseMindSensorsCompassSensor)(transformFrom));
            target.Heading = from.Heading;
            return target;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Compass_I2CResponseMindSensorsCompassSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Compass_Proxy_I2CResponseMindSensorsCompassSensor(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.I2CResponseMindSensorsCompassSensor target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.Proxy.I2CResponseMindSensorsCompassSensor();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.I2CResponseMindSensorsCompassSensor from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Compass.I2CResponseMindSensorsCompassSensor)(transformFrom));
            target.Heading = from.Heading;
            return target;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_AccelerometerConfig_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_AccelerometerConfig(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.AccelerometerConfig target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.AccelerometerConfig();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.AccelerometerConfig from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.AccelerometerConfig)(transformFrom));
            target.Name = from.Name;
            target.SensorPort = from.SensorPort;
            target.PollingFrequencyMs = from.PollingFrequencyMs;
            return target;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_AccelerometerConfig_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_AccelerometerConfig(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.AccelerometerConfig target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.AccelerometerConfig();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.AccelerometerConfig from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.AccelerometerConfig)(transformFrom));
            target.Name = from.Name;
            target.SensorPort = from.SensorPort;
            target.PollingFrequencyMs = from.PollingFrequencyMs;
            return target;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_AccelerometerState_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_AccelerometerState(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.AccelerometerState target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.AccelerometerState();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.AccelerometerState from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.AccelerometerState)(transformFrom));
            target.Connected = from.Connected;
            target.Name = from.Name;
            target.SensorPort = from.SensorPort;
            target.PollingFrequencyMs = from.PollingFrequencyMs;
            if ((from.ZeroOffset != null)) {
                global::Microsoft.Robotics.Services.Sample.Lego.Nxt.Common.AccelerometerReading tmp = new global::Microsoft.Robotics.Services.Sample.Lego.Nxt.Common.AccelerometerReading();
                ((Microsoft.Dss.Core.IDssSerializable)(from.ZeroOffset)).CopyTo(((Microsoft.Dss.Core.IDssSerializable)(tmp)));
                target.ZeroOffset = tmp;
            }
            else {
                target.ZeroOffset = null;
            }
            if ((from.Tilt != null)) {
                global::Microsoft.Robotics.Services.Sample.Lego.Nxt.Common.AccelerometerReading tmp0 = new global::Microsoft.Robotics.Services.Sample.Lego.Nxt.Common.AccelerometerReading();
                ((Microsoft.Dss.Core.IDssSerializable)(from.Tilt)).CopyTo(((Microsoft.Dss.Core.IDssSerializable)(tmp0)));
                target.Tilt = tmp0;
            }
            else {
                target.Tilt = null;
            }
            return target;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_AccelerometerState_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_AccelerometerState(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.AccelerometerState target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.AccelerometerState();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.AccelerometerState from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.AccelerometerState)(transformFrom));
            target.Connected = from.Connected;
            target.Name = from.Name;
            target.SensorPort = from.SensorPort;
            target.PollingFrequencyMs = from.PollingFrequencyMs;
            global::Microsoft.Robotics.Services.Sample.Lego.Nxt.Common.AccelerometerReading tmp = from.ZeroOffset;
            if ((tmp != null)) {
                global::Microsoft.Robotics.Services.Sample.Lego.Nxt.Common.AccelerometerReading tmp0 = new global::Microsoft.Robotics.Services.Sample.Lego.Nxt.Common.AccelerometerReading();
                ((Microsoft.Dss.Core.IDssSerializable)(tmp)).CopyTo(((Microsoft.Dss.Core.IDssSerializable)(tmp0)));
                target.ZeroOffset = tmp0;
            }
            global::Microsoft.Robotics.Services.Sample.Lego.Nxt.Common.AccelerometerReading tmp1 = from.Tilt;
            if ((tmp1 != null)) {
                global::Microsoft.Robotics.Services.Sample.Lego.Nxt.Common.AccelerometerReading tmp2 = new global::Microsoft.Robotics.Services.Sample.Lego.Nxt.Common.AccelerometerReading();
                ((Microsoft.Dss.Core.IDssSerializable)(tmp1)).CopyTo(((Microsoft.Dss.Core.IDssSerializable)(tmp2)));
                target.Tilt = tmp2;
            }
            return target;
        }
        
        private static global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.I2CReadMindSensorsAccelerationSensor _cachedInstance4 = new global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.I2CReadMindSensorsAccelerationSensor();
        
        private static global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.I2CReadMindSensorsAccelerationSensor _cachedInstance3 = new global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.I2CReadMindSensorsAccelerationSensor();
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_I2CReadMindSensorsAccelerationSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_I2CReadMindSensorsAccelerationSensor(object transformFrom) {
            return _cachedInstance3;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_I2CReadMindSensorsAccelerationSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_I2CReadMindSensorsAccelerationSensor(object transformFrom) {
            return _cachedInstance4;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_I2CResponseMindSensorsAccelerationSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_I2CResponseMindSensorsAccelerationSensor(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.I2CResponseMindSensorsAccelerationSensor target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.I2CResponseMindSensorsAccelerationSensor();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.I2CResponseMindSensorsAccelerationSensor from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.I2CResponseMindSensorsAccelerationSensor)(transformFrom));
            target.X = from.X;
            target.Y = from.Y;
            target.Z = from.Z;
            return target;
        }
        
        public static object Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_I2CResponseMindSensorsAccelerationSensor_TO_Microsoft_Robotics_Services_Sample_MindSensors_Accelerometer_Proxy_I2CResponseMindSensorsAccelerationSensor(object transformFrom) {
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.I2CResponseMindSensorsAccelerationSensor target = new global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.Proxy.I2CResponseMindSensorsAccelerationSensor();
            global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.I2CResponseMindSensorsAccelerationSensor from = ((global::Microsoft.Robotics.Services.Sample.MindSensors.Accelerometer.I2CResponseMindSensorsAccelerationSensor)(transformFrom));
            target.X = from.X;
            target.Y = from.Y;
            target.Z = from.Z;
            return target;
        }
    }
}
