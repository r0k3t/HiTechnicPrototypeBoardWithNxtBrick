<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: ColorSensor.xslt $ $Revision: 2 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:ls="http://schemas.microsoft.com/robotics/2010/03/lego/nxt/colorsensor.html">
  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        <xsl:text>NXT Color Sensor - </xsl:text>
        <xsl:value-of select="/ls:ColorSensorState/ls:Name"/>
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        NXT Color Sensor
      </xsl:with-param>
      <xsl:with-param name="description">
        Status of an NXT Color Sensor.
      </xsl:with-param>
      <xsl:with-param name="head">
        <script language="javascript" type="text/javascript" src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Common.js"/>
        <script language="javascript" type="text/javascript">
          <xsl:text>
var pollingFrequency = </xsl:text>
          <xsl:value-of select="/ls:ColorSensorState/ls:PollingFrequencyMs"/>
          <xsl:text>;
var timeStamp = "</xsl:text>
          <xsl:value-of select="/ls:ColorSensorState/ls:TimeStamp"/>
          <xsl:text>";
</xsl:text>

          <![CDATA[

function onState(state)
{
    if (state.TimeStamp != timeStamp)
    {
      var reading;

      timeStamp = state.TimeStamp;
      document.all("timestampCell").innerText = timeStamp;
      reading = parseInt(state.Reading);
      if (state.SensorMode == "Color")
      {
        switch (reading)
        {
          case 1:
            document.all("readingCell").innerText = state.Reading + " (Black)";
            document.all("valueCell").style.backgroundColor = "rgb(0, 0, 0)";
            break;
          case 2:
            document.all("readingCell").innerText = state.Reading + " (Blue)";
            document.all("valueCell").style.backgroundColor = "rgb(13, 105, 171)";
            break;
          case 3:
            document.all("readingCell").innerText = state.Reading + " (Green)";
            document.all("valueCell").style.backgroundColor = "rgb(75, 151, 74)";
            break;
          case 4:
            document.all("readingCell").innerText = state.Reading + " (Yellow)";
            document.all("valueCell").style.backgroundColor = "rgb(245, 205, 47)";
            break;
          case 5:
            document.all("readingCell").innerText = state.Reading + " (Red)";
            document.all("valueCell").style.backgroundColor = "rgb(196, 40, 27)";
            break;
          case 6:
            document.all("readingCell").innerText = state.Reading + " (White)";
            document.all("valueCell").style.backgroundColor = "rgb(242, 243, 242)";
            break;
          default:
            document.all("readingCell").innerText = state.Reading + " (Unknown)";
            document.all("valueCell").style.backgroundColor = "rgb(255, 255, 255)";
            break;
        }
      }
      else
      {
        reading = reading / 10.0;
        document.all("readingCell").innerText = state.Reading;
        document.all("valueCell").style.backgroundColor = "rgb(" + reading + "%, " + reading + "%, " + reading + "%)";
      }
      
      var color;
      var highlight;
      
      document.all("sensorModeCell").innerText = state.SensorMode;
      if (state.SensorMode == "Color")
      {
        color = "#CCC";
        highlight = "#FFF";
      }
      else if (state.SensorMode == "Red")
      {
        color = "#C00";
        highlight = "#F66";
      }
      else if (state.SensorMode == "Green")
      {
        color = "#0C0";
        highlight = "#6F6";
      }
      else if (state.SensorMode == "Blue")
      {
        color = "#00C";
        highlight = "#66F";
      }
      else
      {
        color = "#333";
        highlight = "#666";
      }
      
      document.all("colorCell").style.backgroundColor = color;
      document.all("highlightCell").style.backgroundColor = highlight;
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

  <xsl:template match="/ls:ColorSensorState">
    <img src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Resources.NxtColorSensor.Image.png"
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
        <th>Sensor Mode</th>
        <td id="sensorModeCell">
          <xsl:value-of select="ls:SensorMode"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Reading:</th>
        <td id="readingCell">
          <xsl:value-of select="ls:Reading"/>
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
              <xsl:when test="ls:SensorMode = 'Color'">
                <xsl:text>#CCC</xsl:text>
              </xsl:when>
              <xsl:when test="ls:SensorMode = 'Red'">
                <xsl:text>#C00</xsl:text>
              </xsl:when>
              <xsl:when test="ls:SensorMode = 'Green'">
                <xsl:text>#0C0</xsl:text>
              </xsl:when>
              <xsl:when test="ls:SensorMode = 'Blue'">
                <xsl:text>#00C</xsl:text>
              </xsl:when>
              <xsl:otherwise>
                <xsl:text>#333</xsl:text>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <xsl:variable name="highlight">
            <xsl:choose>
              <xsl:when test="ls:SensorMode = 'Color'">
                <xsl:text>#FFF</xsl:text>
              </xsl:when>
              <xsl:when test="ls:SensorMode = 'Red'">
                <xsl:text>#F66</xsl:text>
              </xsl:when>
              <xsl:when test="ls:SensorMode = 'Green'">
                <xsl:text>#6F6</xsl:text>
              </xsl:when>
              <xsl:when test="ls:SensorMode = 'Blue'">
                <xsl:text>#66F</xsl:text>
              </xsl:when>
              <xsl:otherwise>
                <xsl:text>#666</xsl:text>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <xsl:variable name="reading">
            <xsl:text>rgb(</xsl:text>
            <xsl:value-of select="ls:Reading div 10"/>
            <xsl:text>%, </xsl:text>
            <xsl:value-of select="ls:Reading div 10"/>
            <xsl:text>%, </xsl:text>
            <xsl:value-of select="ls:Reading div 10"/>
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
            <div id ="valueCell" style="position:absolute;left:180px;top:15px;width:60px;height:60px;border:solid 1 black;overflow:hidden;background-color: {$reading};"/>
          </div>
        </td>
      </tr>
    </table>
  </xsl:template>
</xsl:stylesheet>