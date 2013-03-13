<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: LightSensor.xslt $ $Revision: 5 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:ls="http://schemas.microsoft.com/robotics/2007/07/lego/nxt/lightsensor.html">
  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        <xsl:text>NXT Light Sensor - </xsl:text>
        <xsl:value-of select="/ls:LightSensorState/ls:Name"/>
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        NXT Light Sensor
      </xsl:with-param>
      <xsl:with-param name="description">
        Status of an NXT Light Sensor.
      </xsl:with-param>
      <xsl:with-param name="head">
        <script language="javascript" type="text/javascript" src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Common.js"/>
        <script language="javascript" type="text/javascript">
          <xsl:text>
var pollingFrequency = </xsl:text>
          <xsl:value-of select="/ls:LightSensorState/ls:PollingFrequencyMs"/>
          <xsl:text>;
var timeStamp = "</xsl:text>
          <xsl:value-of select="/ls:LightSensorState/ls:TimeStamp"/>
          <xsl:text>";
</xsl:text>

          <![CDATA[

function onState(state)
{
    if (state.TimeStamp != timeStamp)
    {
      timeStamp = state.TimeStamp;
      document.all("timestampCell").innerText = timeStamp;
      document.all("intensityCell").innerText = state.Intensity;
      
      var color;
      var highlight;
      
      if (state.SpotlightOn == "true")
      {
        document.all("spotlightCell").innerText = "On";
        color = "#C00";
        highlight = "#FFF";
      }
      else
      {
        document.all("spotlightCell").innerText = "Off";
        color = "#600";
        highlight = "#C66";
      }
      
      intensity = parseInt(state.Intensity);
      
      document.all("colorCell").style.backgroundColor = color;
      document.all("highlightCell").style.backgroundColor = highlight;
      document.all("valueCell").style.backgroundColor = "rgb(" + intensity + "%, " + intensity + "%, " + intensity + "%)";
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

  <xsl:template match="/ls:LightSensorState">
    <img src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Resources.NxtLightSensor.Image.png"
         align="right" alt="Buttons" width="32px" height="32px"/>
    <table>
      <tr class="even">
        <th width="30%">Name:</th>
        <td width="70%">
          <xsl:value-of select="ls:Name"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Connection:</th>
        <td>
          <xsl:choose>
            <xsl:when test="ls:Connected = 'true'">
              Connected
            </xsl:when>
            <xsl:otherwise>
              Not connected
            </xsl:otherwise>
          </xsl:choose>
          <xsl:text> on </xsl:text>
          <xsl:value-of select="ls:SensorPort"/>
        </td>
      </tr>
      <tr class="even">
        <th>Spotlight</th>
        <td id="spotlightCell">
          <xsl:choose>
            <xsl:when test="ls:SpotlightOn = 'true'">
              <xsl:text>On</xsl:text>
            </xsl:when>
            <xsl:otherwise>
              <xsl:text>Off</xsl:text>
            </xsl:otherwise>
          </xsl:choose>
        </td>
      </tr>
      <tr class="odd">
        <th>Intensity:</th>
        <td id="intensityCell">
          <xsl:value-of select="ls:Intensity"/>
        </td>
      </tr>
      <tr class="even">
        <th>Timestamp:</th>
        <td id="timestampCell">
          <xsl:value-of select="ls:TimeStamp"/>
        </td>
      </tr>
      <tr class="even">
        <td colspan="2">
          <xsl:variable name="color">
            <xsl:choose>
              <xsl:when test="ls:SpotlightOn = 'true'">
                <xsl:text>#C00</xsl:text>
              </xsl:when>
              <xsl:otherwise>
                <xsl:text>#600</xsl:text>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <xsl:variable name="highlight">
            <xsl:choose>
              <xsl:when test="ls:SpotlightOn = 'true'">
                <xsl:text>#FFF</xsl:text>
              </xsl:when>
              <xsl:otherwise>
                <xsl:text>#C66</xsl:text>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <xsl:variable name="intensity">
            <xsl:text>rgb(</xsl:text>
            <xsl:value-of select="ls:Intensity"/>
            <xsl:text>%, </xsl:text>
            <xsl:value-of select="ls:Intensity"/>
            <xsl:text>%, </xsl:text>
            <xsl:value-of select="ls:Intensity"/>
            <xsl:text>%)</xsl:text>
          </xsl:variable>
          <div style="position:relative;width:435px;height:100px;margin:10px">
            <div style="position:absolute;left:0px;top:5px;height:80px;width:160px;color:#888;background-color:#888">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
              <div style="position:absolute;overflow:hidden;left:0px;top:70px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#00000000',EndColorStr='#40000000')" />
            </div>
            <div style="position:absolute;left:20px;top:0px;height:45px;width:120px;color:#DDD;background-color:#DDD">
              <div style="position:absolute;overflow:hidden;left:0px;top:0px;width:100%;height:10px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')" />
            </div>
            <div id="colorCell" style="position:absolute;left:152px;top:37px;width:16px;height:16px;overflow:hidden;background-color:{$color}">
              <div id="highlightCell" style="position:absolute;left:10px;top:2px;width:4px;height:4px;overflow:hidden;background-color:{$highlight}"/>
            </div>
            <div id ="valueCell" style="position:absolute;left:180px;top:15px;width:60px;height:60px;border:solid 1 black;overflow:hidden;background-color: {$intensity};"/>
          </div>
        </td>
      </tr>
    </table>
  </xsl:template>
</xsl:stylesheet>