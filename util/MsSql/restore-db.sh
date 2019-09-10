#!/bin/bash
BACKUP_FILENAME=$(basename $1)

BACKUPS_ROOT="/etc/bytegarden/mssql/backups"
BACKUP_PATH="${BACKUPS_ROOT}/${BACKUP_FILENAME}"

if [[ ! -f ${BACKUP_PATH} ]]; then
    echo "Backup file doesn't exists: ${BACKUP_FILENAME}"
    exit 1
fi

/opt/mssql-tools/bin/sqlcmd \
    -S localhost \
    -U sa -P ${SA_PASSWORD} \
    -Q "RESTORE DATABASE [vault] FROM DISK='${BACKUP_PATH}' WITH STATS=10"