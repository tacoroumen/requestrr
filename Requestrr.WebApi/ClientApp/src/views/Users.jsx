import { useEffect, useState } from "react";
import { useDispatch } from 'react-redux';
import { getLinkedUsers, saveLinkedUsers, getOverseerrUsers } from "../store/actions/LinkedUsersActions";

// reactstrap components
import {
  Alert,
  Badge,
  Card,
  CardHeader,
  CardBody,
  FormGroup,
  Form,
  Input,
  InputGroup,
  Container,
  Row,
  Col,
  Table
} from "reactstrap";
// core components
import UserHeader from "../components/Headers/UserHeader.jsx";

const PAGE_SIZE = 50;

const isMatrixId = value => /^@[^\s:]+:\S+$/.test(value);
const isDiscordId = value => /^\d{5,20}$/.test(value);


function Users() {
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [saveAttempted, setSaveAttempted] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [loadError, setLoadError] = useState("");
  const [userLinks, setUserLinks] = useState([]);
  const [overseerrUsers, setOverseerrUsers] = useState([]);
  const [overseerrUsersError, setOverseerrUsersError] = useState("");
  const [search, setSearch] = useState("");
  const [newIds, setNewIds] = useState({});
  const [rowErrors, setRowErrors] = useState({});
  const [shownCount, setShownCount] = useState(PAGE_SIZE);

  const dispatch = useDispatch();


  useEffect(() => {
    Promise.all([dispatch(getLinkedUsers()), dispatch(getOverseerrUsers())])
      .then(([links, users]) => {
        setIsLoading(false);

        if (links.ok) {
          setUserLinks(links.userLinks.map(x => ({chatUserId: x.chatUserId || "", overseerrUserId: x.overseerrUserId || ""})));
        } else {
          setLoadError(links.error);
        }

        if (users.ok) {
          setOverseerrUsers(users.users);
        } else {
          setOverseerrUsersError(users.error);
        }
      });
  }, []);


  // Links to Seerr users that no longer exist (or could not be loaded) still get a row, so they can be removed.
  const knownIds = new Set(overseerrUsers.map(x => x.id));
  const rows = [
    ...overseerrUsers,
    ...[...new Set(userLinks.map(x => x.overseerrUserId))]
      .filter(id => !knownIds.has(id))
      .map(id => ({id: id, name: `Seerr user ${id}`, email: overseerrUsersError ? "" : "Not found in Seerr"}))
  ];

  const query = search.trim().toLowerCase();
  const linksOf = id => userLinks.filter(x => x.overseerrUserId === id);
  const filteredRows = !query ? rows : rows.filter(user =>
    user.name.toLowerCase().includes(query)
    || (user.email || "").toLowerCase().includes(query)
    || user.id === query
    || linksOf(user.id).some(link => link.chatUserId.toLowerCase().includes(query)));
  const shownRows = filteredRows.slice(0, shownCount);


  const setRowError = (id, error) => setRowErrors({...rowErrors, [id]: error});

  const addLink = id => {
    const chatUserId = (newIds[id] || "").trim();

    if (!isMatrixId(chatUserId) && !isDiscordId(chatUserId)) {
      setRowError(id, "Enter a Matrix ID (@name:your-domain) or a Discord user ID (a number).");
      return;
    }

    const existing = userLinks.find(x => x.chatUserId.toLowerCase() === chatUserId.toLowerCase());

    if (existing) {
      const owner = rows.find(x => x.id === existing.overseerrUserId);
      setRowError(id, `${chatUserId} is already linked to ${owner ? owner.name : "another Seerr user"}.`);
      return;
    }

    setUserLinks([...userLinks, {chatUserId: chatUserId, overseerrUserId: id}]);
    setNewIds({...newIds, [id]: ""});
    setRowError(id, "");
  };

  const removeLink = chatUserId => setUserLinks(userLinks.filter(x => x.chatUserId !== chatUserId));


  const onSaving = e => {
    e.preventDefault();

    if (isSaving) {
      return;
    }

    setIsSaving(true);

    dispatch(saveLinkedUsers(userLinks))
      .then(data => {
        setIsSaving(false);
        setSaveAttempted(true);
        setSaveSuccess(data.ok);
        setSaveError(data.ok ? "" : data.error);
      });
  };


  return (
    <>
      <UserHeader title="Users" description="This page is for linking Discord and Matrix accounts to Seerr users" />
      <Container className="mt--7" fluid>
        <Row>
          <Col className="order-xl-1" xl="12">
            <Card className="bg-secondary shadow">
              <CardHeader className="bg-white border-0">
                <Row className="align-items-center">
                  <Col xs="8">
                    <h3 className="mb-0">Seerr users</h3>
                  </Col>
                </Row>
              </CardHeader>
              <CardBody className={isLoading ? "fade" : "fade show"}>
                <Form className="complex" onSubmit={e => e.preventDefault()}>
                  <p className="text-muted text-sm">
                    Requests from a linked account are made as that Seerr user, so they show up under that person in Seerr and follow their permissions and quota.
                    A Seerr user can have several accounts, such as one on Discord and one on Matrix.
                    For Discord, use the user ID (Developer Mode on, right-click the user, Copy User ID). A link here wins over the Discord ID saved in Seerr.
                    Unlinked accounts request as the default Seerr user set under Download Clients.
                  </p>
                  {
                    loadError || overseerrUsersError ? (
                      <Alert className="mt-3" color="warning">
                        <strong>{loadError || overseerrUsersError}</strong>
                      </Alert>)
                      : null
                  }
                  <FormGroup>
                    <Input
                      className="form-control-alternative"
                      type="search"
                      placeholder="Search by name, email or linked account"
                      value={search}
                      onChange={e => { setSearch(e.target.value); setShownCount(PAGE_SIZE); }}
                    />
                  </FormGroup>
                  <div className="table-responsive">
                    <Table className="align-items-center table-flush bg-white" size="sm">
                      <thead className="thead-light">
                        <tr>
                          <th scope="col">Seerr user</th>
                          <th scope="col">Linked accounts</th>
                          <th scope="col" style={{minWidth: "320px"}}>Link an account</th>
                        </tr>
                      </thead>
                      <tbody>
                        {
                          shownRows.map(user => (
                            <tr key={user.id}>
                              <td>
                                <div className="font-weight-bold">{user.name}</div>
                                <div className="text-muted text-sm">{user.email}</div>
                              </td>
                              <td style={{whiteSpace: "normal"}}>
                                {
                                  linksOf(user.id).length === 0
                                    ? <span className="text-muted text-sm">None</span>
                                    : linksOf(user.id).map(link => (
                                      <Badge key={link.chatUserId} color={isMatrixId(link.chatUserId) ? "success" : "primary"} className="mr-2 mb-1" style={{fontSize: "0.8rem", textTransform: "none"}}>
                                        {isMatrixId(link.chatUserId) ? "Matrix" : "Discord"}: {link.chatUserId}
                                        <button type="button" className="close ml-2" style={{float: "none", fontSize: "1rem", lineHeight: 1}}
                                                aria-label={`Unlink ${link.chatUserId}`} onClick={() => removeLink(link.chatUserId)}>
                                          <span aria-hidden="true">&times;</span>
                                        </button>
                                      </Badge>
                                    ))
                                }
                              </td>
                              <td>
                                <InputGroup size="sm">
                                  <Input
                                    type="text"
                                    placeholder="Matrix or Discord ID"
                                    value={newIds[user.id] || ""}
                                    onChange={e => setNewIds({...newIds, [user.id]: e.target.value})}
                                    onKeyDown={e => { if (e.key === "Enter") { e.preventDefault(); addLink(user.id); } }}
                                  />
                                  <button className="btn btn-sm btn-outline-primary ml-2" type="button" onClick={() => addLink(user.id)}>
                                    Link
                                  </button>
                                </InputGroup>
                                {
                                  rowErrors[user.id]
                                    ? <div className="text-danger text-sm mt-1" style={{whiteSpace: "normal"}}>{rowErrors[user.id]}</div>
                                    : null
                                }
                              </td>
                            </tr>
                          ))
                        }
                        {
                          !isLoading && filteredRows.length === 0
                            ? <tr><td colSpan="3" className="text-muted text-sm">{query ? "No Seerr users match your search." : "No Seerr users found."}</td></tr>
                            : null
                        }
                      </tbody>
                    </Table>
                  </div>
                  {
                    filteredRows.length > shownRows.length
                      ? <div className="text-center mt-3">
                        <button className="btn btn-sm btn-outline-primary" type="button" onClick={() => setShownCount(shownCount + PAGE_SIZE)}>
                          Show more ({filteredRows.length - shownRows.length} left)
                        </button>
                      </div>
                      : null
                  }
                  <Row className="mt-4">
                    <Col>
                      <FormGroup>
                        {
                          saveAttempted && !isSaving ?
                            !saveSuccess ? (
                              <Alert className="text-center" color="danger">
                                <strong>{saveError}</strong>
                              </Alert>)
                              : <Alert className="text-center" color="success">
                                <strong>Linked accounts updated successfully.</strong>
                              </Alert>
                            : null
                        }
                      </FormGroup>
                      <FormGroup className="text-right">
                        <button className="btn btn-icon btn-3 btn-primary" onClick={onSaving} disabled={isSaving || isLoading || !!loadError} type="button">
                          <span className="btn-inner--icon"><i className="fas fa-save"></i></span>
                          <span className="btn-inner--text">Save Changes</span>
                        </button>
                      </FormGroup>
                    </Col>
                  </Row>
                </Form>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    </>
  );
}

export default Users;
