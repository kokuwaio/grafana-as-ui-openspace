FROM grafana/grafana:9.4.7

## Set Grafana options
ENV GF_ENABLE_GZIP=true
ENV GF_USERS_DEFAULT_THEME=dark
ENV GF_FEATURE_TOGGLES_ENABLE=topnav,newPanelChromeUI

## Enable Anonymous Authentication
ENV GF_AUTH_ANONYMOUS_ENABLED=false
ENV GF_AUTH_BASIC_ENABLED=false

## Disable Sanitize
ENV GF_PANELS_DISABLE_SANITIZE_HTML=true

## Disable Explore
ENV GF_EXPLORE_ENABLED=false

## Disable Alerting
ENV GF_ALERTING_ENABLED=false
ENV GF_UNIFIED_ALERTING_ENABLED=false

## Set Home Dashboard
ENV GF_DASHBOARDS_DEFAULT_HOME_DASHBOARD_PATH=/etc/grafana/provisioning/dashboards/gateways.json

## Paths
ENV GF_PATHS_PROVISIONING="/etc/grafana/provisioning"
ENV GF_PATHS_PLUGINS="/var/lib/grafana/plugins"

## Copy artifacts
## Required for the Application plugin
#COPY --chown=grafana:root dist /app
#COPY entrypoint.sh /

## Copy Provisioning
COPY --chown=grafana:root provisioning $GF_PATHS_PROVISIONING

##################################################################
## Customization depends on the Grafana version
## May work or not work for the version different from the current
## Check GitHub file history for the previous Grafana versions
##################################################################
USER root

## Replace Favicon
COPY images/favicon-32x32.png /usr/share/grafana/public/img/fav32.png

## Replace Logo
COPY images/inoa_logo.svg /usr/share/grafana/public/img/grafana_icon.svg

## Update Background
#COPY img/background.svg /usr/share/grafana/public/img/g8_login_dark.svg
#COPY img/background.svg /usr/share/grafana/public/img/g8_login_light.svg


## Update Help menu
RUN sed -i "s|\[\[.NavTree\]\],|nav,|g; \
    s|window.grafanaBootData = {| \
    let nav = [[.NavTree]]; \
    nav[nav.length -1]['subTitle'] = 'Application'; \
    window.grafanaBootData = {|g" \
    /usr/share/grafana/public/views/index.html

## Update Title
RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|"AppTitle","Grafana")|"AppTitle","Inoa")|g' {} \;

## Update Login Title
RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|LoginTitle="Welcome to Grafana"|LoginTitle="Welcome to Inoa"|g' {} \;

## Remove Documentation, Support, Community in the Footer
RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|\[{target:"_blank",id:"documentation".*grafana_footer"}\]|\[\]|g' {} \;

## Remove Edition in the Footer
RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|({target:"_blank",id:"version",.*licenseUrl})|()|g' {} \;

## Remove Version in the Footer
RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|({target:"_blank",id:"version",.*CHANGELOG.md":void 0})|()|g' {} \;

## Remove New Version is available in the Footer
RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|({target:"_blank",id:"updateVersion",.*grafana_footer"})|()|g' {} \;

## Update Search Place holder in the Top Navigation
RUN find /usr/share/grafana/public/build/ -name *.js -exec sed -i 's|("nav.search.placeholder","Search Grafana")|("nav.search.placeholder","Search")|g' {} \;
##################################################################

RUN grafana-cli plugins install marcusolsson-json-datasource
RUN grafana-cli plugins install yesoreyeram-infinity-datasource


USER grafana

## Entrypoint
#ENTRYPOINT [ "/bin/bash", "/entrypoint.sh" ]