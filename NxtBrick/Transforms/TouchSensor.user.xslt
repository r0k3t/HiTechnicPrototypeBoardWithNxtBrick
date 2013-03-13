<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: TouchSensor.user.xslt $ $Revision: 6 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:ts="http://schemas.microsoft.com/robotics/2007/07/lego/nxt/touchsensor.user.html">

  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        <xsl:text>NXT Touch Sensor - </xsl:text>
        <xsl:value-of select="/ts:TouchSensorState/ts:Name"/>
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        NXT Touch Sensor
      </xsl:with-param>
      <xsl:with-param name="description">
        Status of an NXT Touch Sensor.
      </xsl:with-param>
      <xsl:with-param name="head">
        <script language="javascript" type="text/javascript" src="/resources/User.NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Common.js"/>
        <script language="javascript" type="text/javascript">
          <xsl:text>
var pollingFrequency = </xsl:text>
          <xsl:value-of select="/ts:TouchSensorState/ts:PollingFrequencyMs"/>
          <xsl:text>;
var timeStamp = "</xsl:text>
          <xsl:value-of select="/ts:TouchSensorState/ts:TimeStamp"/>
          <xsl:text>";
</xsl:text>
          
          <![CDATA[

function onState(state)
{
    if (state.TimeStamp != timeStamp)
    {
      timeStamp = state.TimeStamp;
      document.all("timestampCell").innerText = timeStamp;
      
      var button = document.all("buttonCell");
      var pressed = document.all("pressedCell");
      
      if (state.TouchSensorOn == "true")
      {
        pressed.innerText = "Pressed";
        button.style.backgroundColor = "#FED";
      }
      else
      {
        pressed.innerText = "Released";
        button.style.backgroundColor = "orange";
      }
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

  <xsl:template match="/ts:TouchSensorState">
    <img src="/resources/User.NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Resources.NxtTouchSensor.Image.png"
         align="right" alt="Buttons" width="32px" height="32px"/>
    <table>
      <tr class="even">
        <th>Name:</th>
        <td>
          <xsl:value-of select="ts:Name"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Connection:</th>
        <td>
          <xsl:choose>
            <xsl:when test="ts:Connected = 'true'">
              <xsl:text>Connected</xsl:text>
            </xsl:when>
            <xsl:otherwise>
              <xsl:text>Not connected</xsl:text>
            </xsl:otherwise>
          </xsl:choose>
          <xsl:text> on </xsl:text>
          <xsl:value-of select="ts:SensorPort"/>
        </td>
      </tr>
      <tr class="even">
        <th>Current State:</th>
        <td id="pressedCell">
          <xsl:choose>
            <xsl:when test="ts:TouchSensorOn = 'true'">
              <xsl:text>Pressed</xsl:text>
            </xsl:when>
            <xsl:otherwise>
              <xsl:text>Released</xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </td>
      </tr>
      <tr class="odd">
        <th>Timestamp:</th>
        <td id="timestampCell">
          <xsl:value-of select="ts:TimeStamp"/>
        </td>
      </tr>
      <tr class="even">
        <td colspan="2">
          <xsl:variable name="color">
            <xsl:choose>
              <xsl:when test="ts:TouchSensorOn = 'true'">
                <xsl:text>#FED</xsl:text>
              </xsl:when>
              <xsl:otherwise>
                <xsl:text>orange</xsl:text>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <div style="position:relative;width:180px;height:85px;margin:10px">
            <div style="position:absolute;left:0px;top:5px;height:80px;width:160px;color:#888;background-color:#888">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
              <div style="position:absolute;overflow:hidden;left:0px;top:70px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#00000000',EndColorStr='#40000000')" />
            </div>
            <div style="position:absolute;left:20px;top:0px;height:45px;width:120px;color:#DDD;background-color:#DDD">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
            </div>
            <div style="position:absolute;left:160px;top:25px;width:5px;height:40px;color:orange;background-color:orange">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
              <div style="position:absolute;overflow:hidden;left:0px;top:30px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#00000000',EndColorStr='#40000000')" />
            </div>
            <div id="buttonCell" style="position:absolute;left:165px;top:25px;width:15px;height:40px;background-color:{$color}">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
              <div style="position:absolute;overflow:hidden;left:0px;top:30px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#00000000',EndColorStr='#40000000')" />
              <div style="position:absolute;overflow:hidden;left:10px;top:0px;width:5px;height:100%;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=1,StartColorStr='#00000000',EndColorStr='#40000000')" />
            </div>
          </div>
        </td>
      </tr>
    </table>
  </xsl:template>
</xsl:stylesheet>