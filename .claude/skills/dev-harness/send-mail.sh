#!/bin/bash
# Send a test email to GreenMail via SMTP
# Usage: send-mail.sh [from] [to] [subject] [body]
set -euo pipefail

FROM="${1:-newsletter@example.com}"
TO="${2:-admin@feirb.local}"
SUBJECT="${3:-Test: Newsletter subscription confirmation}"
BODY="${4:-This is a test email for classification testing.}"

python3 -c "
import smtplib
from email.mime.text import MIMEText
from email.utils import formatdate

msg = MIMEText('$BODY')
msg['Subject'] = '$SUBJECT'
msg['From'] = '$FROM'
msg['To'] = '$TO'
msg['Date'] = formatdate(localtime=True)

with smtplib.SMTP('localhost', 3025) as s:
    s.send_message(msg)
print('Email sent: $FROM -> $TO, Subject: $SUBJECT')
"
