<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: Drive.xslt $ $Revision: 5 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:d="http://schemas.microsoft.com/robotics/2007/07/lego/nxt/drive.html">
  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">
        NXT Drive
      </xsl:with-param>
      <xsl:with-param name="serviceName">
        NXT Drive
      </xsl:with-param>
      <xsl:with-param name="description">
        Status of the NXT Drive service.
      </xsl:with-param>
      <xsl:with-param name="head">
        <script language="javascript" type="text/javascript" src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Common.js"/>
        <script language="javascript" type="text/javascript">
          <xsl:text>
var pollingFrequency = </xsl:text>
          <xsl:value-of select="/d:DriveState/d:PollingFrequencyMs"/>
          <xsl:text>;
var timeStamp = "</xsl:text>
          <xsl:value-of select="/d:DriveState/d:TimeStamp"/>
          <xsl:text>";
</xsl:text>

          <![CDATA[

function onState(state)
{
    if (state.TimeStamp != timeStamp)
    {
      timeStamp = state.TimeStamp;
      document.all('timeStamp').innerText = timeStamp;
      
      if (state.RuntimeStatistics != undefined)
      {
        for (var key in state.RuntimeStatistics)
        {
          var cell = document.all(key);
          if (cell != null)
          {
            cell.innerText = state.RuntimeStatistics[key];
          }
        }
        
        var left = parseInt(state.RuntimeStatistics.LeftPowerCurrent * 100.0);
        var right = parseInt(state.RuntimeStatistics.RightPowerCurrent * 100.0);
        setPowerBar(document.all("leftPowerDiv").style, left);
        setPowerBar(document.all("rightPowerDiv").style, right);
      }
    }
    readState(pollingFrequency);
}

function setPowerBar(style, power)
{
  if (power > 0)
  {
    style.top = (120 - power) + "px";
    style.height = power + "px";
  }
  else if (power < 0)
  {
    style.top = "122px";
    style.height = (-power) + "px";
  }
  else
  {
    style.top = "120px";
    style.height = "2px";
  }
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

  <xsl:template match="/d:DriveState">
    <img src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Resources.NxtDrive.Image.png"
         align="right" alt="Buttons" width="32px" height="32px"/>
    <table>
      <tr class="even">
        <th>Connected</th>
        <td colspan="2">
          <xsl:value-of select="d:Connected"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Distance between wheels</th>
        <td colspan="2">
          <xsl:call-template name="displayMetricLength">
            <xsl:with-param name="value" select="d:DistanceBetweenWheels"/>
          </xsl:call-template>
        </td>
      </tr>
      <tr class="even">
        <th>Timestamp</th>
        <td id="timeStamp" colspan="2" style="width:200pt">
          <xsl:value-of select="d:TimeStamp"/>
        </td>
      </tr>
      <tr>
        <th>Wheels</th>
        <th>Left</th>
        <th>Right</th>
      </tr>
      <tr class="even">
        <th>Port</th>
        <td>
          <xsl:value-of select="d:LeftWheel/d:MotorPort"/>
        </td>
        <td>
          <xsl:value-of select="d:RightWheel/d:MotorPort"/>
        </td>
      </tr>
      <tr class="odd">
        <th>Polarity</th>
        <td>
          <xsl:choose>
            <xsl:when test="d:LeftWheel/d:ReversePolarity = 'true'">
              Reversed
            </xsl:when>
            <xsl:otherwise>
              Normal
            </xsl:otherwise>
          </xsl:choose>
        </td>
        <td>
          <xsl:choose>
            <xsl:when test="d:RightWheel/d:ReversePolarity = 'true'">
              Reversed
            </xsl:when>
            <xsl:otherwise>
              Normal
            </xsl:otherwise>
          </xsl:choose>
        </td>
      </tr>
      <tr class="even">
        <th>Wheel Diameter</th>
        <td>
          <xsl:call-template name="displayMetricLength">
            <xsl:with-param name="value" select="d:LeftWheel/d:WheelDiameter"/>
          </xsl:call-template>
        </td>
        <td>
          <xsl:call-template name="displayMetricLength">
            <xsl:with-param name="value" select="d:RightWheel/d:WheelDiameter"/>
          </xsl:call-template>
        </td>
      </tr>
      <xsl:if test="d:PollingFrequencyMs &gt; 0">
        <xsl:apply-templates select="d:RuntimeStatistics"/>
      </xsl:if>
    </table>
    
  </xsl:template>

  <xsl:template match="d:RuntimeStatistics">
    <tr class="odd">
      <th>Current Power</th>
      <td id="LeftPowerCurrent">
        <xsl:value-of select="d:LeftPowerCurrent"/>
      </td>
      <td id="RightPowerCurrent">
        <xsl:value-of select="d:RightPowerCurrent"/>
      </td>
    </tr>
    <tr class="even">
      <th>Target Power</th>
      <td id="LeftPowerTarget">
        <xsl:value-of select="d:LeftPowerTarget"/>
      </td>
      <td id="RightPowerTarget">
        <xsl:value-of select="d:RightPowerTarget"/>
      </td>
    </tr>
    <tr class="odd">
      <th>RPM</th>
      <td id="LeftMotorRpm">
        <xsl:value-of select="d:LeftMotorRpm"/>
      </td>
      <td id="RightMotorRpm">
        <xsl:value-of select="d:RightMotorRpm"/>
      </td>
    </tr>
    <tr class="even">
      <th>Current Encoder</th>
      <td id="LeftEncoderCurrent">
        <xsl:value-of select="d:LeftEncoderCurrent"/>
      </td>
      <td id="RightEncoderCurrent">
        <xsl:value-of select="d:RightEncoderCurrent"/>
      </td>
    </tr>
    <tr class="odd">
      <th>Encoder Target</th>
      <td id="LeftEncoderTarget">
        <xsl:value-of select="d:LeftEncoderTarget"/>
      </td>
      <td id="RightEncoderTarget">
        <xsl:value-of select="d:RightEncoderTarget"/>
      </td>
    </tr>
    <tr>
      <td colspan="3">
        <div style="position:relative;width:200px;height:242px;background:#DDD;border:solid 1 #44A;">
          <div style="position:absolute;left:39px;top:19px;width:42px;height:204px;background:#AAF;border:solid 1 #008;"/>
          <div style="position:absolute;left:119px;top:19px;width:42px;height:204px;background:#AAF;border:solid 1 #008;"/>
          <div style="position:absolute;left:20px;top:120px;width:160px;height:2px;background:#888;overflow:hidden;"/>
          <div id="leftPowerDiv">
            <xsl:attribute name="style">
              <xsl:text>position:absolute;left:40px;width:40px;background:#44A;overflow:hidden;</xsl:text>
              <xsl:choose>
                <xsl:when test="d:LeftPowerCurrent &gt; 0">
                  <xsl:value-of select="concat('top:',120 - d:LeftPowerCurrent,'px;height:',d:LeftPowerCurrent,'px;')"/>
                </xsl:when>
                <xsl:when test="d:LeftPowerCurrent &lt; 0">
                  <xsl:value-of select="concat('top:',122 + d:LeftPowerCurrent,'px;height:',-d:LeftPowerCurrent,'px;')"/>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>top:120px;height:2px;</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:attribute>
          </div>
          <div id="rightPowerDiv">
            <xsl:attribute name="style">
              <xsl:text>position:absolute;left:120px;width:40px;background:#44A;overflow:hidden;</xsl:text>
              <xsl:choose>
                <xsl:when test="d:RightPowerCurrent &gt; 0">
                  <xsl:value-of select="concat('top:',120 - d:RightPowerCurrent,'px;height:',d:RightPowerCurrent,'px;')"/>
                </xsl:when>
                <xsl:when test="d:RightPowerCurrent &lt; 0">
                  <xsl:value-of select="concat('top:',122 + d:RightPowerCurrent,'px;height:',-d:RightPowerCurrent,'px;')"/>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>top:120px;height:2px;</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </xsl:attribute>
          </div>
          <div style="position:absolute;background:transparent;left:40px;top:20px;width:15px;height:202px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=1,StartColorStr='#60FFFFFF',EndColorStr='#00FFFFFF')"/>
          <div style="position:absolute;background:transparent;left:120px;top:20px;width:15px;height:202px;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=1,StartColorStr='#60FFFFFF',EndColorStr='#00FFFFFF')"/>
        </div>
      </td>
    </tr>
  </xsl:template>

  <xsl:template name="displayMetricLength">
    <xsl:param name="value"/>
    <xsl:choose>
      <xsl:when test="$value &lt; 0.01">
        <xsl:value-of select="format-number($value * 1000,'0.###')"/>
        <xsl:text>mm (</xsl:text>
        <xsl:value-of select="format-number($value div 0.0254,'0.###')"/>
        <xsl:text>&quot;)</xsl:text>
      </xsl:when>
      <xsl:when test="$value &lt; 1">
        <xsl:value-of select="format-number($value * 100,'0.#')"/>
        <xsl:text>cm (</xsl:text>
        <xsl:value-of select="format-number($value div 0.0254,'0.#')"/>
        <xsl:text>&quot;)</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$value"/>
        <xsl:text>m (</xsl:text>
        <xsl:value-of select="format-number($value div 0.0254,'0')"/>
        <xsl:text>&quot;)</xsl:text>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>