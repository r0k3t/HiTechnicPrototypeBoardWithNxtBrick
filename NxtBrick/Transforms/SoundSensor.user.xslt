<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: SoundSensor.user.xslt $ $Revision: 7 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:ss="http://schemas.microsoft.com/robotics/2007/07/lego/nxt/soundsensor.user.html">
  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        <xsl:text>NXT Sound Sensor - </xsl:text>
        <xsl:value-of select="/ss:SoundSensorState/ss:Name"/>
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        NXT Sound Sensor
      </xsl:with-param>
      <xsl:with-param name="description">
        Status of an NXT Sound Sensor.
      </xsl:with-param>
      <xsl:with-param name="head">
        <script language="javascript" type="text/javascript" src="/resources/User.NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Common.js"/>
        <script language="javascript" type="text/javascript">
          <xsl:text>
var pollingFrequency = </xsl:text>
          <xsl:value-of select="/ss:SoundSensorState/ss:PollingFrequencyMs"/>
          <xsl:text>;
var timeStamp = "</xsl:text>
          <xsl:value-of select="/ss:SoundSensorState/ss:TimeStamp"/>
          <xsl:text>";
var intensity = </xsl:text>
          <xsl:value-of select="/ss:SoundSensorState/ss:Intensity"/>
          <xsl:text>;
var average = intensity;
</xsl:text>

          <![CDATA[

function onState(state)
{
    if (state.TimeStamp != timeStamp)
    {
      timeStamp = state.TimeStamp;
      document.all("timestampCell").innerText = timeStamp;
      document.all("intensityCell").innerText = state.Intensity;
      
      intensity = parseInt(state.Intensity);
      average = (intensity + 2 * average) / 3;
      
      var idiv = document.all("intensity");
      var adiv = document.all("average");
      
      idiv.style.top = ((100 - intensity) * 8 / 10) + "px";
      adiv.style.top = (5 + (100 - average) * 8 / 10) + "px";
      adiv.style.height = (average * 8 / 10) + "px";
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

  <xsl:template match="/ss:SoundSensorState">
    <img src="/resources/User.NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Resources.NxtSoundSensor.Image.png"
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
        <th>Intensity:</th>
        <td id="intensityCell">
          <xsl:value-of select="ss:Intensity"/>
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
          <div style="position:relative;width:435px;height:100px;margin:10px">
            <div style="position:absolute;left:0px;top:5px;height:80px;width:160px;color:#888;background-color:#888">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
              <div style="position:absolute;overflow:hidden;left:0px;top:70px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#00000000',EndColorStr='#40000000')" />
            </div>
            <div style="position:absolute;left:20px;top:0px;height:45px;width:120px;color:#DDD;background-color:#DDD">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
            </div>
            <div style="position:absolute;left:145px;top:15px;width:12px;height:15px;overflow:hidden;background-color:orange;"/>
            <div style="position:absolute;left:145px;top:37px;width:12px;height:15px;overflow:hidden;background-color:orange;"/>
            <div style="position:absolute;left:125px;top:59px;width:32px;height:15px;overflow:hidden;background-color:orange;"/>
            <div id="average" style="position:absolute;left:180px;top:{5 + (100 - ss:Intensity) * 8 div 10}px;width:40px;height:{ss:Intensity * 8 div 10}px;overflow:hidden;background-color:#CCC"/>
            <div id="intensity" style="position:absolute;left:180px;top:{(100 - ss:Intensity) * 8 div 10}px;width:40px;height:5px;overflow:hidden;background-color:#888"/>
          </div>
        </td>
      </tr>
    </table>
  </xsl:template>
</xsl:stylesheet>