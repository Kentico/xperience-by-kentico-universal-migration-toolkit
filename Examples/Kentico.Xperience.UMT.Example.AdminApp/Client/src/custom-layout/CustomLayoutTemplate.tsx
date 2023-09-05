import React, { useState } from "react";
import { ActionTile, Box, Button, ButtonSize, Callout, CalloutPlacementType, CalloutType, Cols, Column, Divider, DividerOrientation, DropDownActionMenu, FileInput, Headline, HeadlineSize, Input, MenuItem, NotificationBarAlert, ProgressBar, RadioButton, RadioGroup, RadioGroupSize, Row, Select, Shelf, Spacing, Stack, TreeNodeMenuAction, UploadTile, UploadTileSize } from "@kentico/xperience-admin-components";


/*
* This file demonstrates a custom UI page template.
  The template supports a single page command that retrieves a string value from the backend.

  In this example, the command retrieves the server's DateTime.Now value and displays it in a label.
  See ~\UIPages\CustomTemplate\CustomTemplate.cs for the backend definition of the page.
*/

interface CustomLayoutProps {
  readonly label: string;
}

let canContinue = true;
let toofast = false;

export const CustomLayoutTemplate = ({ label }: CustomLayoutProps) => {
  const [file, setFile] = useState<File | null>(null);
  const [state, setState] = useState<string[]>([]);
  const [currentFile, setCurrentFile] = useState({ current: 0, total: 0 });
  const [stats, setStats] = useState<{ [statKey: string]: number } | null>(null);

  function parseFile(this: any, file: File, callback: (buffer: ArrayBuffer) => boolean, finishedCallback: () => void) {
    var fileSize = file.size;
    var chunkSize = 32 * 1024; // bytes
    var offset = 0;
    var self = this as unknown; // we need a reference to the current object
    var chunkReaderBlock: null | ((_offset: number, length: number, _file: File) => void) = null;

    var readEventHandler = function (evt: ProgressEvent<FileReader>) {
      if (!canContinue) {
        return;
      }

      if (evt.target == null) {
        console.log('progress is null');
        return;
      }
      if (evt.target.error == null && evt.target.result != null) {
        // offset += (evt.target.result as any).length;
        offset += chunkSize;
        if (!callback(evt.target.result as ArrayBuffer)) // callback for handling read chunk
        {
          return;
        }
      } else {
        finishedCallback();
        console.log("Read error: " + evt.target.error);
        return;
      }
      if (offset >= fileSize) {
        finishedCallback();
        console.log("Done reading file");
        return;
      }

      // of to the next chunk
      if (chunkReaderBlock != null && canContinue) {
        // console.log('reading next block');
        chunkReaderBlock(offset, chunkSize, file);
      }
    }

    chunkReaderBlock = function (_offset: number, length: number, _file: File) {
      var r = new FileReader();
      var blob = _file.slice(_offset, length + _offset);
      r.onload = readEventHandler;
      if (toofast) {
        setTimeout(() => { r.readAsText(blob); }, 3000);
        console.log('too fast => wait 3s');
        toofast = false;
      }
      else {
        r.readAsText(blob);
      }
    }

    // now let's start the read with the first block
    chunkReaderBlock(offset, chunkSize, file);
  }

  const onUpload = () => {
    setStats(null);
    setState([]);
    if (file != null) {
      console.log(file);
      let port = location.port != '' ? `:${location.port}` : '';
      const socket = new WebSocket('ws://' + location.hostname + `${port}/umtsample/ws`);
      socket.binaryType = 'blob';
      socket.onerror = (e) => {
        console.log('error occured', e);
        canContinue = false;
        console.log('closing socket => error');
        socket.close();
      }
      socket.onmessage = (event) => {
        var p = JSON.parse(event.data);
        switch (p.type) {
          case "stats": {
            try {
              setStats(JSON.parse(p.payload));
            }
            catch (e) {
              console.error(e);
            }
            break;
          }
          case "headerConfirmed": {
            console.log('header confirmed, starting import');
            parseFile(file, (buffer) => {
              // console.log('chunk');     
              if (socket.readyState == socket.OPEN) {
                socket.send(buffer);
                return true;
              }
              else {
                return false;
              }
            }, () => {
              console.log('sending end sequence');
              var endMessage = Uint8Array.of(0x2D, 0x2D, 0x2D, 0x2D, 0x2D);
              socket.send(endMessage);

              setTimeout(() => {
                // socket.send("---FINISHED---");
                console.log('closing socket - timeout 60s');
                socket.close();
              }, 60000);
            });
            break;
          }
          case "toofast": {
            toofast = true;
            break;
          }
          case "msg": {
            setState((prev) => [
              ...prev,
              p.payload
            ])
            break;
          }
          case "progress": {
            const len = parseInt(p.payload);
            setCurrentFile(prev => ({
              total: prev.total,
              current: prev.current + len
            }))
            break;
          }
          case "finished": {
            console.log('closing socket');
            canContinue = false;
            if (socket.readyState < socket.CLOSING) {
              socket.close();
            }
            break;
          }
        }
      };
      socket.onopen = function (event) {
        canContinue = true;
        setState((prev) => [
          ...prev,
          `Sending file of length: ${file.size}`
        ])
        console.log('connected');
        setCurrentFile({
          total: file.size,
          current: 0
        })

        socket.send(JSON.stringify({
          type: 'header',
          payload: {
          }
        }));
      };
    } else {
      alert('no file selected');
    }
  };

  const newInterface = (
    <Box spacing={Spacing.M}>
      <Headline size={HeadlineSize.L} spacingBottom={Spacing.M}>
        {label}
      </Headline>
      <Row spacing={Spacing.XL}>
        <Column
          cols={Cols.Col12}
          colsMd={Cols.Col10}
          colsLg={Cols.Col8}
          order={Cols.Col2}
          orderLg={Cols.Col1}
        >
          <Stack spacing={Spacing.XL} fullHeight={true}>
            <Callout
              type={CalloutType.QuickTip}
              placement={CalloutPlacementType.OnPaper}
              headline="Basic usage"
              subheadline="Info"
            >
              Component <a target="_blank" href="https://github.com/Kentico/xperience-by-kentico-universal-migration-toolkit/blob/main/Docs/README.md">documentation</a> and <a target="_blank" href="https://github.com/Kentico/xperience-by-kentico-universal-migration-toolkit/tree/main/Docs/Samples">samples</a> 
            </Callout>

            <div style={{ display: 'inline-block' }}>
              <ActionTile label={"Legacy Kentico"} icon={"xp-cb-check"}></ActionTile>&nbsp;
              <ActionTile label={"Sitecore"} icon={"xp-cb-check"}></ActionTile>&nbsp;
              <ActionTile label={"Sitefinity"} icon={"xp-cb-check"}></ActionTile>&nbsp;
              <ActionTile label={"Umbraco"} icon={"xp-cb-check"}></ActionTile>
            </div>

            <UploadTile
              acceptFiles=".json"
              firstLineLabel="Drag&Drop .json here"
              secondLineLabel="or"
              buttonLabel="Browse"
              size={UploadTileSize.Full}
              onUpload={([f]) => {
                if (f instanceof File) {
                  setFile(f);
                  setCurrentFile({ current: 0, total: f.size });
                }
              }}
            />


            {file !== null && (
              <p style={{ color: 'var(--color-text-default-on-light)' }}>
                File Selected: {file.name}
              </p>
            )}



            {currentFile.total > 0 && (
              <div>
                <Headline size={HeadlineSize.S} spacingBottom={Spacing.M}>
                  Upload Progress
                </Headline>
                <ProgressBar
                  completed={Math.floor(
                    (currentFile.current / currentFile.total) * 100,
                  )}
                />
              </div>
            )}

            <Button
              label="Import data"
              size={ButtonSize.S}
              onClick={onUpload}
            />

            <Divider orientation={DividerOrientation.Horizontal} />


            <Shelf>
              {stats && <Box spacing={Spacing.M}>
                <Headline size={HeadlineSize.S}>Imported objects</Headline>
                <pre style={{ color: 'var(--color-text-default-on-light)' }}>
                  {JSON.stringify(stats, null, 4)}
                </pre>
              </Box>}
              {state.length > 0 && (<Box spacing={Spacing.M}>
                <Headline size={HeadlineSize.S}>Import Log</Headline>
                <pre style={{ color: 'var(--color-text-default-on-light)' }}>
                  {[...state].reverse().join('\r\n')}
                </pre>
              </Box>)}
            </Shelf>

          </Stack>
        </Column>
      </Row>
    </Box>
  );

  return newInterface;
};
