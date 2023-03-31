# grafana-as-ui-openspace

OpenSpace session to determine the usage of Grafana as a real world frontend to be used by end user

- Überhaupt ne gute Idee?
- Theming / Whitelabeling?
- Authentication?
- Referenz: Gatewayregistry bauen auf Basis der Inoa API

## Findings

### Positive

Es gibt mehrere Ansätze:
- Theming und manipulation des stock Grafana (z.B. Ausblenden von Links, Logos, Menüpunkten, etc.)
- Verwendung von speziellen Plugins (Data Manipulation, Canvas, ...)
- Eigene App und / oder Plugins für Grafana schreiben

### Negative

- Man muss beim Update von Grafana ggf. die Whitelabeling Arbeit nachmal machen
- Deeplinks in bestimmte Funktionen gehen eventuell immernoch


## TODO

- Tabelle mit Einträgen der Gateways
- Klick auf Tabelleneintrag führt zur Detailsseite der Gateways
- Detailseite:
  - Labels anschauen
  - Gerät aufmalen mit Canvas und Zähler auflisten am Gerät