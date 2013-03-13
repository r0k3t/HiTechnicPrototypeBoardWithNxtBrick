<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: SonarSensor.user.xslt $ $Revision: 5 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:ss="http://schemas.microsoft.com/robotics/2007/07/lego/nxt/sonarsensor.user.html">
  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        <xsl:text>NXT Ultrasonic Sensor - </xsl:text>
        <xsl:value-of select="/ss:SonarSensorState/ss:Name"/>
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        NXT Ultrasonic Sensor
      </xsl:with-param>
      <xsl:with-param name="description">
        Status of an NXT Ultrasonic Sensor.
      </xsl:with-param>
      <xsl:with-param name="head">
        <script language="javascript" type="text/javascript" src="/resources/User.NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Common.js"/>
        <script language="javascript" type="text/javascript">
          <xsl:text>
var pollingFrequency = </xsl:text>
          <xsl:value-of select="/ss:SonarSensorState/ss:PollingFrequencyMs"/>
          <xsl:text>;
var timeStamp = "</xsl:text>
          <xsl:value-of select="/ss:SonarSensorState/ss:TimeStamp"/>
          <xsl:text>";
</xsl:text>

          <![CDATA[

function onState(state)
{
    if (state.TimeStamp != timeStamp)
    {
      timeStamp = state.TimeStamp;
      document.all("timestampCell").innerText = timeStamp;
      
      var fill = document.all("fillCell");
      var distance = document.all("distanceCell");
      var object = document.all("objectCell");
      
      if (state.Distance == "255")
      {
        distance.innerText = "255 (No Object Detected)";
      }
      else
      {
        distance.innerText = state.Distance;
      }
      fill.style.width = state.Distance + "px";
      object.style.left = state.Distance + "px";
    }
    readState(pollingFrequency);
}

function startPolling()
{
  setStateCompletion("onState");
  readState(pollingFrequency);
}

setTimeout("startPolling()", 1000);

          ]]>
        </script>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <!--<xsl:output method="html"/>-->

  <xsl:template match="/ss:SonarSensorState">
    <img src="/resources/User.NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Resources.NxtUltrasonicSensor.Image.png"
         align="right" alt="Buttons" width="32px" height="32px"/>
    <table>
      <tr class="even">
        <th width="30%">Name:</th>
        <td width="70%">
          <xsl:value-of select="ss:Name"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Connection:</th>
        <td>
          <xsl:choose>
            <xsl:when test="ss:Connected = 'true'">
              Connected
            </xsl:when>
            <xsl:otherwise>
              Not connected
            </xsl:otherwise>
          </xsl:choose>
          <xsl:text> on </xsl:text>
          <xsl:value-of select="ss:SensorPort"/>
        </td>
      </tr>
      <tr class="even">
        <th>Distance:</th>
        <td id="distanceCell">
          <xsl:choose>
            <xsl:when test="ss:Distance = 255">
              <xsl:text>255 (No Object Detected)</xsl:text>
            </xsl:when>
            <xsl:otherwise>
              <xsl:value-of select="ss:Distance"/>
            </xsl:otherwise>
          </xsl:choose>
        </td>
      </tr>
      <tr class="odd">
        <th>Timestamp:</th>
        <td id="timestampCell">
          <xsl:value-of select="ss:TimeStamp"/>
        </td>
      </tr>
      <tr class="even">
        <td colspan="2">
          <div style="position:relative;width:435px;height:85px;margin:10px">
            <div style="position:absolute;left:0px;top:5px;height:80px;width:140px;color:#888;background-color:#888">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
              <div style="position:absolute;overflow:hidden;left:0px;top:70px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#00000000',EndColorStr='#40000000')" />
            </div>
            <div style="position:absolute;left:140px;top:5px;height:60px;width:20px;color:#888;background-color:#888">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
              <div style="position:absolute;overflow:hidden;left:0px;top:50px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#00000000',EndColorStr='#40000000')" />
            </div>
            <div style="position:absolute;left:20px;top:0px;height:45px;width:120px;color:#DDD;background-color:#DDD">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
            </div>
            <div style="position:absolute;left:160px;top:15px;width:5px;height:40px;color:orange;background-color:orange">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
              <div style="position:absolute;overflow:hidden;left:0px;top:30px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#00000000',EndColorStr='#40000000')" />
            </div>
            <div style="position:absolute;left:170px;top:5px;height:60px;width:260px;vertical-align:middle;">
              <div id="fillCell"  style="position:absolute;top:0px;height:60px;left:0px;width:{ss:Distance}px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=1,StartColorStr='#00000000',EndColorStr='#20000000')"/>
              <div style="position:absolute;top:0px;height:60px;left:255px;width:5px;background-color:#DDD"/>
              <div id="objectCell" style="position:absolute;top:0px;height:60px;left:{ss:Distance}px;width:5px;background-color:#AAA"/>
            </div>
          </div>
        </td>
      </tr>
    </table>
  </xsl:template>
</xsl:stylesheet>